using SiiRk.Helpers;
using SiiRk.Measures;
using SiiRk.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SiiRk.ViewModels
{
    public partial class ApplicationViewModel : Observable
    {
        private IEnumerable<Node> GenerateRecommendations()
        {
            SetRateForAllUsers();

            var users = Users.Except(new [] { CurrentUser }).ToList();

            users.Sort(delegate (User user1, User user2)
            {
                if (user1 == null && user2 == null) return 0;
                if (user1 == null) return 1;
                if (user2 == null) return -1;

                if (user1.Rate == user2.Rate) return 0;
                if (user1.Rate > user2.Rate) return -1;
                return 1;
            });

            //users = users.Take(UsersCount).ToList();

            var recommendations = new List<Node>();

            foreach (var diff in users.Select(user => user.Favourite.Except(CurrentUser.Favourite)))
            {
                recommendations.AddRange(diff);
            }

            return recommendations.Distinct().Except(CurrentUser.NotShow);
        }

        private void SetRateForAllUsers()
        {
            foreach (var user in Users.Except(new [] { CurrentUser }))
            {
                user.Rate = user.Favourite.Intersect(CurrentUser.Favourite).Count();
            }
        }

        private void AddRecommendationsToItems(IEnumerable<Node> nodes)
        {
            RecommendationsItems = new ObservableCollection<TreeViewItem>();
            Recommendations = new ObservableCollection<Node>(nodes);

            foreach (var node in Recommendations)
            {
                var item = new TreeViewItem();
                var menu = new ContextMenu();

                menu.Items.Add(new MenuItem
                {
                    Header = "Больше не предлагать",
                    Command = AddToNotShowCommand,
                    CommandParameter = item,
                });
                menu.Items.Add(new MenuItem
                {
                    Header = "Добавить в сохраненные",
                    Command = AddToFavouriteCommand,
                    CommandParameter = item,
                });

                item.ContextMenu = menu;
                item.Header = node.ToString();
                Tree.AddNodeToTreeView(item, node);
                RecommendationsItems.Add(item);
            }
        }

        private IEnumerable<Node> GenerateRecsForSingleNode(Node node)
        {
            var nodes = new List<Node>();
            
            MemoryTree.ToList(nodes);
            nodes = nodes.Except(CurrentUser.NotShow).Except(new[] { node }).ToList();

            foreach (var n in nodes)
            {
                var calculator = new DistanceCalculator(node, n);
                n.Distance = calculator.CalculateDistance((MeasureType)MeasureTypeIndex);
            }

            nodes.Sort(delegate (Node node1, Node node2)
            {
                if (node1 == null && node2 == null) return 0;
                if (node1 == null) return -1;
                if (node2 == null) return 1;

                if (node1.Distance == node2.Distance) return 0;
                if (node1.Distance > node2.Distance) return 1;
                return -1;
            });

            return nodes;
        }

        private IEnumerable<Node> GenerateRecsForNodeArray()
        {
            var nodes = new List<Node>();

            MemoryTree.ToList(nodes);
            nodes = nodes.Except(CurrentUser.NotShow).Except(CurrentUser.Favourite).ToList();

            foreach (var node in nodes)
            {
                node.Distance = 0.0;

                foreach (var n in CurrentUser.Favourite)
                {
                    var calculator = new DistanceCalculator(node, n);
                    node.Distance += calculator.CalculateDistance((MeasureType)MeasureTypeIndex);
                }

                node.Distance /= (double)CurrentUser.Favourite.Count();
            }

            nodes.Sort(delegate (Node node1, Node node2)
            {
                if (node1 == null && node2 == null) return 0;
                if (node1 == null) return -1;
                if (node2 == null) return 1;

                if (node1.Distance == node2.Distance) return 0;
                if (node1.Distance > node2.Distance) return 1;
                return -1;
            });

            return nodes;
        }
    }
}
