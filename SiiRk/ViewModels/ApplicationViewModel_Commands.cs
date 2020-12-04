using Microsoft.Win32;
using SiiRk.Helpers;
using SiiRk.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SiiRk.Search;

namespace SiiRk.ViewModels
{
    public partial class ApplicationViewModel : Observable
    {
        private ICommand _openFileDialogCommand;
        private ICommand _loadTreeCommand;
        private ICommand _addUserCommand;
        private ICommand _changeUserCommand;
        private ICommand _deleteUserCommand;

        private ICommand _generateRecommendationsCommand;
        private ICommand _generateRecsForSingleNodeCommand;
        private ICommand _generateRecsForNodeArrayCommand;
        
        private ICommand _searchCommand;
        private ICommand _workCommand;

        private ICommand _addToFavouriteCommand;
        private ICommand _addToNotShowCommand;
        private ICommand _removeFromFavouriteCommand;
        private ICommand _removeFromNotShowCommand;

        public ICommand OpenFileDialogCommand => _openFileDialogCommand ??= new RelayCommand<object>(_ =>
        {
            var fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog().Value)
            {
                Filename = fileDialog.FileName;
            }
        });

        public ICommand LoadTreeCommand => _loadTreeCommand ??= new RelayCommand<object>(_ =>
        {
            LoadTreeFromFile();
        });

        public ICommand AddUserCommand => _addUserCommand ??= new RelayCommand<string>(username =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show($"Некорректное имя пользователя.", "Ошибка");
            }
            else if (Users.Any(u => u.Name == username))
            {
                MessageBox.Show($"Пользователь с именем {username} уже существует.", "Ошибка");
            }
            else
            {
                AddUser(new User(username));
            }
        });

        public ICommand ChangeUserCommand => _changeUserCommand ??= new RelayCommand<string>(username =>
        {
            var user = Users.SingleOrDefault(u => u.Name == username);
            if (user == null)
            {
                MessageBox.Show($"Пользователя с именем {username} не существует.", "Ошибка");
            }
            else
            {
                ChangeUser(user);
            }
        });

        public ICommand DeleteUserCommand => _deleteUserCommand ??= new RelayCommand<string>(username =>
        {
            var user = Users.SingleOrDefault(u => u.Name == (username ?? throw new ArgumentNullException(nameof(username))));
            if (user == null)
            {
                MessageBox.Show($"Пользователя с именем {username} не существует.", "Ошибка");
            }
            else RemoveUser(user);
        });

        public ICommand GenerateRecommendationsCommand =>
            _generateRecommendationsCommand ??= new RelayCommand<object>(_ =>
            {
                if (CurrentUser == null)
                {
                    MessageBox.Show("Не выбран пользователь.", "Ошибка");
                }
                else
                {
                    var nodes = GenerateRecommendations();
                    AddRecommendationsToItems(nodes);
                }
            });

        public ICommand SearchCommand => _searchCommand ??= new RelayCommand<object>(_ =>
        {
            try
            {
                IEnumerable<Node> result = ParametricSearch.Search(new SearchParams
                {
                    Tree = MemoryTree,
                    SpeedInterval = new Interval(MinSpeed, MaxSpeed),
                    CapacityInterval = new Interval(MinCapacity, MaxCapacity),
                    YearInterval = new Interval(MinReleaseYear, MaxReleaseYear),
                    CostInterval = new Interval(MinCost, MaxCost),
                    ApplicationType = (Application == 2) ? (bool?)null : (Application == 0),
                    MemoryType = (MemoryType == 5) ? null : (MemoryType?)MemoryType,
                    Count = RecsLength
                });

                AddRecommendationsToItems(result);
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;
                MessageBox.Show(e.Message, "Ошибка");
            }
        });

        public ICommand WorkCommand => _workCommand ??= new RelayCommand<object>(_ =>
        {
            try
            {
                if (CurrentUser == null)
                {
                    MessageBox.Show("Не выбран пользователь.", "Ошибка");
                }
                else if (!CollaborativeOption && !ContentOption && !SearchOption)
                {
                    MessageBox.Show("Не выбран режим работы.");
                }
                else
                {
                    var nodes = new List<Node>();

                    if (CollaborativeOption)
                    {
                        nodes.AddRange(GenerateRecommendations().Take(RecsLength));
                    }

                    if (ContentOption)
                    {
                        if (nodes.Any())
                        {
                            nodes = nodes.Intersect(GenerateRecsForNodeArray().Take(RecsLength)).ToList();
                        }
                        else
                        {
                            nodes.AddRange(GenerateRecsForNodeArray().Take(RecsLength));
                        }
                    }

                    if (SearchOption)
                    {
                        var searchParams = new SearchParams
                        {
                            Tree = MemoryTree,
                            SpeedInterval = new Interval(MinSpeed, MaxSpeed),
                            CapacityInterval = new Interval(MinCapacity, MaxCapacity),
                            YearInterval = new Interval(MinReleaseYear, MaxReleaseYear),
                            CostInterval = new Interval(MinCost, MaxCost),
                            ApplicationType = (Application == 2) ? (bool?) null : (Application == 0),
                            MemoryType = (MemoryType == 5) ? null : (MemoryType?) MemoryType,
                            Count = RecsLength
                        };

                        if (nodes.Any())
                        {
                            nodes = nodes.Intersect(ParametricSearch.Search(searchParams).Take(RecsLength)).ToList();
                        }
                        else
                        {
                            nodes.AddRange(ParametricSearch.Search(searchParams).Take(RecsLength));
                        }
                    }

                    if (nodes.Any())
                    {
                        AddRecommendationsToItems(nodes.Take(RecsLength));
                    }
                    else
                    {
                        MessageBox.Show("Ничего не найдено.");
                    }
                }
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;
                MessageBox.Show(e.Message, "Ошибка");
            }
        });

        public ICommand GenerateRecsForSingleNodeCommand =>
            _generateRecsForSingleNodeCommand ??= new RelayCommand<TreeViewItem>(item =>
        {
            var node = MemoryTree.GetNode(item.Header.ToString().Split('\n').First());

            if (node == null)
            {
                MessageBox.Show("Узел с указанным именем не найден в дереве.", "Ошибка");
            }
            else
            {
                var nodes = GenerateRecsForSingleNode(node).Take(RecsLength);
                AddRecommendationsToItems(nodes);
            }
        });


        public ICommand GenerateRecsForNodeArrayCommand => 
            _generateRecsForNodeArrayCommand ??= new RelayCommand<object>(_ => 
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Не выбран пользователь.", "Ошибка");
            }
            else if (!CurrentUser.Favourite.Any())
            {
                MessageBox.Show("Ничего не сохранено.", "Ошибка");
            }
            else
            {
                var nodes = GenerateRecsForNodeArray().Take(RecsLength);
                AddRecommendationsToItems(nodes);
            }
        });

        public ICommand AddToFavouriteCommand => _addToFavouriteCommand ??= new RelayCommand<TreeViewItem>(item =>
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Не выбран пользователь.", "Ошибка");
            }
            else
            {
                var name = item.Header.ToString().Split('\n').First();
                var node = MemoryTree.GetNode(name);

                if (node == null)
                {
                    MessageBox.Show("Узел с указанным именем не найден в дереве.", "Ошибка");
                }
                else if (CurrentUser.Favourite.Any(n => n.Name == name))
                {
                    MessageBox.Show("Узел с указанным именем уже в списке.", "Ошибка");
                }
                else AddToFavourite(node);
            }
        });

        public ICommand RemoveFromFavouriteCommand =>
            _removeFromFavouriteCommand ??= new RelayCommand<TreeViewItem>(item =>
            {
                if (CurrentUser == null)
                {
                    MessageBox.Show("Не выбран пользователь.", "Ошибка");
                }
                else
                {
                    var node = CurrentUser.Favourite.SingleOrDefault(n => n.Name == item.Header.ToString().Split('\n').First());

                    if (node == null)
                    {
                        MessageBox.Show("Узел с указанным именем не найден в списке.", "Ошибка");
                    }
                    else RemoveFromFavourite(node);
                }
            });

        public ICommand AddToNotShowCommand => _addToNotShowCommand ??= new RelayCommand<TreeViewItem>(item =>
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Не выбран пользователь.", "Ошибка");
            }
            else
            {
                var name = item.Header.ToString().Split('\n').First();
                var node = MemoryTree.GetNode(name);

                if (node == null)
                {
                    MessageBox.Show("Узел с указанным именем не найден в дереве.", "Ошибка");
                }
                else if (CurrentUser.NotShow.Where(n => n.Name == name).Any())
                {
                    MessageBox.Show("Узел с указанным именем уже в списке.", "Ошибка");
                }
                else AddToNotShow(node);
            }
        });

        public ICommand RemoveFromNotShowCommand =>
            _removeFromNotShowCommand ??= new RelayCommand<TreeViewItem>(item =>
            {
                if (CurrentUser == null)
                {
                    MessageBox.Show("Не выбран пользователь.", "Ошибка");
                }
                else
                {
                    var node = CurrentUser.NotShow
                        .Where(n => n.Name == item.Header.ToString().Split('\n').First()).SingleOrDefault();

                    if (node == null)
                    {
                        MessageBox.Show("Узел с указанным именем не найден в списке.", "Ошибка");
                    }
                    else RemoveFromNotShow(node);
                }
            });
    }
}
