﻿namespace AprioriAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Apriori : IApriori
    {
        readonly Dictionary<string, double> _allFrequentItems;

        public Apriori()
        {
            _allFrequentItems = new Dictionary<string, double>();
        }

        #region IApriori

        Output IApriori.Solve(double minSupport, double minConfidence, IEnumerable<string> items, Dictionary<int, string> transactions)
        {
            Dictionary<string, double> frequentItems = GetL1FrequentItems(minSupport, items, transactions);
            Dictionary<string, double> candidates = new Dictionary<string, double>();
            double transactionsCount = transactions.Count;
            do
            {
                candidates = GenerateCandidates(frequentItems, transactions);
                frequentItems = GetFrequentItems(candidates, minSupport, transactionsCount);
            }
            while (candidates.Count != 0);

            Dictionary<string, Dictionary<string, double>> closedItemSets = GetClosedItemSets(_allFrequentItems);
            List<string> maximalItemSets = GetMaximalItemSets(closedItemSets);
            List<Rule> rules = GenerateRules();
            List<Rule> strongRules = GetStrongRules(minConfidence, rules);
            return new Output
            {
                StrongRules = strongRules,
                MaximalItemSets = maximalItemSets,
                ClosedItemSets = closedItemSets,
                AllFrequentItems = _allFrequentItems
            };
        } 

        #endregion

        #region Private Methods

        private Dictionary<string, double> GetL1FrequentItems(double minSupport, IEnumerable<string> items, Dictionary<int, string> transactions)
        {
            var result = new Dictionary<string, double>();
            double transactionsCount = transactions.Count;
            foreach (var item in items)
            {
                double dSupport = GetSupport(item, transactions);
                if ((dSupport / transactionsCount >= minSupport))
                {
                    result.Add(item, dSupport);
                    _allFrequentItems.Add(item, dSupport);
                }
            }
            return result;
        }

        private double GetSupport(string strGeneratedCandidate, Dictionary<int, string> transactions)
        {
            double support = 0;
            foreach (string transaction in transactions.Values)
            {
                if (CheckIsSubset(strGeneratedCandidate, transaction))
                {
                    support++;
                }
            }
            return support;
        }

        private bool CheckIsSubset(string child, string parent)
        {
            foreach (char c in child)
            {
                if (!parent.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        private Dictionary<string, double> GenerateCandidates(Dictionary<string, double> frequentItems, Dictionary<int, string> transactions)
        {
            Dictionary<string, double> candidates = new Dictionary<string, double>();

            int i = 0;
            foreach (var item in frequentItems.Keys)
            {
                string firstItem = Sort(item);
                for (int j = i + 1; j < frequentItems.Count; j++)
                {
                    string secondItem = Sort(frequentItems.Keys.ElementAt(j));
                    string generatedCandidate = GetCandidate(firstItem, secondItem);
                    if (generatedCandidate != string.Empty)
                    {
                        generatedCandidate = Sort(generatedCandidate);
                        double dSupport = GetSupport(generatedCandidate, transactions);
                        candidates.Add(generatedCandidate, dSupport);
                    }
                }

                i++;
            }

            return candidates;
        }

        private string Sort(string token)
        {
            // Convert to char array, then sort and return
            char[] tokenArray = token.ToCharArray();
            Array.Sort(tokenArray);
            return new string(tokenArray);
        }

        private string GetCandidate(string firstItem, string secondItem)
        {
            int length = firstItem.Length;
            if (length == 1)
            {
                return firstItem + secondItem;
            }
            else
            {
                string firstSubString = firstItem.Substring(0, length - 1);
                string secondSubString = secondItem.Substring(0, length - 1);
                if (firstSubString == secondSubString)
                {
                    return firstItem + secondItem[length - 1];
                }
                return string.Empty;
            }
        }

        private Dictionary<string, double> GetFrequentItems(Dictionary<string, double> candidates, double minSupport, double transactionsCount)
        {
            var result = new Dictionary<string, double>();

            foreach (var item in candidates)
            {
                if ((item.Value / transactionsCount >= minSupport))
                {
                    result.Add(item.Key, item.Value);
                    _allFrequentItems.Add(item.Key, item.Value);
                }
            }

            return result;
        }

        private Dictionary<string, Dictionary<string, double>> GetClosedItemSets(Dictionary<string, double> AllFrequentItems)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();
            int i = 0;
            foreach (var item in AllFrequentItems)
            {
                var parents = GetItemParents(item.Key, i + 1, AllFrequentItems);
                if (IsClosed(item.Key, parents, AllFrequentItems))
                {
                    result.Add(item.Key, parents);
                }
                i++;
            }
            return result;
        }

        private Dictionary<string, double> GetItemParents(string child, int index, Dictionary<string, double> AllFrequentItems)
        {
            var parents = new Dictionary<string, double>();
            for (int j = index; j < AllFrequentItems.Count; j++)
            {
                string parent = AllFrequentItems.Keys.ElementAt(j);
                if (parent.Length == child.Length + 1)
                {
                    if (CheckIsSubset(child, parent))
                    {
                        parents.Add(parent, AllFrequentItems[parent]);
                    }
                }
            }
            return parents;
        }

        private bool IsClosed(string child, Dictionary<string, double> parents, Dictionary<string, double> AllFrequentItems)
        {
            foreach (string parent in parents.Keys)
            {
                if (AllFrequentItems[child] == AllFrequentItems[parent])
                {
                    return false;
                }
            }
            return true;
        }

        private List<string> GetMaximalItemSets(Dictionary<string, Dictionary<string, double>> closedItemSets)
        {
            List<string> maximalItemSets = new List<string>();
            foreach (string item in closedItemSets.Keys)
            {
                var parents = closedItemSets[item];
                if (parents.Count == 0)
                {
                    maximalItemSets.Add(item);
                }
            }
            return maximalItemSets;
        }

        private List<Rule> GenerateRules()
        {
            List<Rule> rules = new List<Rule>();
            foreach (string item in _allFrequentItems.Keys)
            {
                if (item.Length > 1)
                {
                    int maxCombinationLength = item.Length / 2;
                    GenerateCombination(item, maxCombinationLength, ref rules);
                }
            }
            return rules;
        }

        private void GenerateCombination(string item, int combinationLength, ref List<Rule> rules)
        {
            int itemLength = item.Length;
            switch (itemLength)
            {
                case 2:
                    AddItem(item[0].ToString(), item, ref rules);
                    break;
                case 3:
                    for (int i = 0; i < itemLength; i++)
                    {
                        AddItem(item[i].ToString(), item, ref rules);
                    }
                    break;
                default:
                    for (int i = 0; i < itemLength; i++)
                    {
                        GetCombinationRecursive(item[i].ToString(), item, combinationLength, ref rules);
                    }
                    break;
            }
        }

        private void AddItem(string combination, string item, ref List<Rule> rules)
        {
            string remaining = GetRemaining(combination, item);
            Rule rule = new Rule(combination, remaining, 0);
            rules.Add(rule);
        }

        private string GetRemaining(string child, string parent)
        {
            for (int i = 0; i < child.Length; i++)
            {
                int index = parent.IndexOf(child[i]);
                parent = parent.Remove(index, 1);
            }
            return parent;
        }

        private string GetCombinationRecursive(string combination, string item, int combinationLength, ref List<Rule> rules)
        {
            AddItem(combination, item, ref rules);

            char lastTokenCharacter = combination[combination.Length - 1];
            int lastTokenCharcaterIndex = combination.IndexOf(lastTokenCharacter);
            int lastTokenCharcaterIndexInParent = item.IndexOf(lastTokenCharacter);
            char lastItemCharacter = item[item.Length - 1];

            string newToken;
            if (combination.Length == combinationLength)
            {
                if (lastTokenCharacter == lastItemCharacter)
                {
                    return string.Empty;

                }

                combination = combination.Remove(lastTokenCharcaterIndex, 1);
                char nextCharacter = item[lastTokenCharcaterIndexInParent + 1];
                newToken = combination + nextCharacter;

            }
            else
            {
                if (combination != lastItemCharacter.ToString())
                {
                    return string.Empty;
                }

                char cNextCharacter = item[lastTokenCharcaterIndexInParent + 1];
                newToken = combination + cNextCharacter;
            }

            return (GetCombinationRecursive(newToken, item, combinationLength, ref rules));
        }

        private List<Rule> GetStrongRules(double minConfidence, List<Rule> rules)
        {
            List<Rule> strongRules = new List<Rule>();
            foreach (Rule rule in rules)
            {
                string XY = Sort(rule.X + rule.Y);
                AddStrongRule(rule, XY, ref strongRules, minConfidence);
            }
            strongRules.Sort();
            return strongRules;
        }

        private void AddStrongRule(Rule rule, string XY, ref List<Rule> strongRules, double minConfidence)
        {
            double confidence = GetConfidence(rule.X, XY);
            if (confidence >= minConfidence)
            {
                Rule newRule = new Rule(rule.X, rule.Y, confidence);
                strongRules.Add(newRule);
            }
            confidence = GetConfidence(rule.Y, XY);
            if (confidence >= minConfidence)
            {
                Rule newRule = new Rule(rule.Y, rule.X, confidence);
                strongRules.Add(newRule);
            }
        }

        private double GetConfidence(string X, string XY)
        {
            double support_X = _allFrequentItems[X];
            double support_XY = _allFrequentItems[XY];
            return support_XY / support_X;
        }

        #endregion
    }
}