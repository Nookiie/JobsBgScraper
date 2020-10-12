﻿using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobsBgScraper.Common
{
    public static class ScraperManager
    {
        private static readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();

        public static async Task<IEnumerable<HtmlDocument>> ScrapeWebsiteJob()
        {
            cancellationToken.Token.ThrowIfCancellationRequested();
            var web = new HtmlWeb();
            var docs = new Collection<HtmlDocument>();

            foreach (var site in ScraperHelpers.JobSiteUrls)
            {
                docs.Add(await web.LoadFromWebAsync(site));
                cancellationToken.Token.ThrowIfCancellationRequested();
            }

            return docs;
        }

        public static void GetScrapeResultsAndAlertJob(IEnumerable<HtmlDocument> documents)
        {
            List<JobNode> classNodes = new List<JobNode>();

            foreach (var document in documents)
            {
                var positionNodes = document.DocumentNode.SelectNodes("//*[contains(@class, 'joblink')]");
                var companyNodes = document.DocumentNode.SelectNodes("//*[contains(@class, 'company_link')]");

                foreach (var node in positionNodes)
                {
                    var position = node.InnerText.ToLower();
                    string company = null;

                    foreach (var firstTerm in ScraperHelpers.FirstConditionalJobKeyWords)
                    {
                        if (position.Contains(firstTerm))
                        {
                            foreach (var secondTerm in ScraperHelpers.SecondConditionalJobKeyWords)
                            {
                                if (position.Contains(secondTerm))
                                {
                                    var companyNode = node.SelectNodes("../../td/a[contains(@class, 'company_link')]");
                                    company = companyNode[0].InnerText;

                                    FormatNodesJob(position, company, classNodes);
                                }
                            }
                        }
                    }
                }
            }

            PrintResultsJob(classNodes);
            // SaveAsJSON(classNodes);
        }

        private static void FormatNodesJob(string position, string company, List<JobNode> classNodes)
        {
            classNodes.Add(new JobNode(position, company));
        }

        private static string ResultsToStringJob(List<JobNode> collection)
        {
            if (collection.Count == 0)
            {
                return string.Format("There are no available jobs with matching criteria");
            }

            var text = string.Format("Number of available jobs with matching criteria: {0}", collection.Count, "\n");
            var sb = new StringBuilder();

            sb.Append(text);
            sb.Append("\n\n");

            if (collection.Count > 0)
            {
                foreach (var item in collection)
                {
                    sb.Append($"{item.Position}, {item.Company} \n");
                }
            }

            return sb.ToString();
        }

        private static void PrintResultsJob(List<JobNode> collection)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(ResultsToStringJob(collection));
        }

        private static void SaveAsJSONJob(List<JobNode> classNodes)
        {
            string json = JsonConvert.SerializeObject(classNodes.ToArray());

            File.WriteAllText(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/History/history.json", json);
        }
    }
}
