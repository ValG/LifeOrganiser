using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbBuilder
{
    class Program
    {
        private static readonly ushort concurrentTaskNumber = 5;
        private static readonly uint ingredientsNumber = 1000;
        private static List<(string Ingredient, string Original)> ingredients =
            new List<(string Ingredient, string Original)>((int)ingredientsNumber);
        private static ulong recipeIndex = 0;
        private static CancellationTokenSource token = new CancellationTokenSource();

        private static Task exit = new Task(() =>
        {
            do
            {
                while (!Console.KeyAvailable &&
                   ingredients.Count < (ingredientsNumber - concurrentTaskNumber)) ;
            }
            while (ingredients.Count < (ingredientsNumber - concurrentTaskNumber) &&
                   Console.ReadKey(true).Key != ConsoleKey.Q);

            token.Cancel();
        });

        private static List<Task> tasks = new List<Task>(8);

        static void Main(string[] args)
        {
            exit.Start();

            while (!token.IsCancellationRequested)
            {
                LaunchTasks(token.Token).ConfigureAwait(false);
            }

            token.Dispose();

            Task.WhenAll(tasks);

            Console.WriteLine("Press Q to exit...");

            while (Console.ReadKey(true).Key != ConsoleKey.Q) ;

            ingredients.Sort();

            ingredients.Insert(0, ($"Number of ingredients: {ingredients.Count}", string.Empty));
            ingredients.Insert(1, (string.Empty, string.Empty));

            StringBuilder sb = new StringBuilder();

            foreach (var ingredient in ingredients)
            {
                int index = ingredients.IndexOf(ingredient);

                if (ingredients.IndexOf(ingredient) > 1)
                {
                    sb.AppendLine($"{ingredient.Ingredient} <= {ingredient.Original}");
                }
                else
                {
                    sb.AppendLine($"{ingredient.Ingredient}");
                }
            }

            File.WriteAllText(@"C:\Users\Arabella\Downloads\Output.txt", sb.ToString());
        }

        static async Task LaunchTasks(CancellationToken cancellationToken)
        {

            while (tasks.Count < concurrentTaskNumber && !token.IsCancellationRequested)
            {
                var task = new Task(() => GetWebContent("https://www.marmiton.org/recettes/recette-hasard.aspx"));
                task.Start();
                tasks.Add(task);
            }

            var _task = await Task.WhenAny(Task.WhenAll(tasks), exit);

            if (_task != exit)
            {
                foreach (var task in tasks)
                {
                    tasks.Remove(task);
                }
            }
        }

        static void GetWebContent(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || token.IsCancellationRequested)
            {
                return;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (response.CharacterSet == null)
                    {
                        readStream = new StreamReader(receiveStream);
                    }
                    else
                    {
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    }

                    var htmlString = readStream.ReadToEnd();

                    HtmlDocument document = new HtmlDocument();

                    document.LoadHtml(htmlString);

                    HtmlNodeCollection ingredientNodes = document.DocumentNode.SelectNodes("//span[@class='ingredient']");

                    StringBuilder sb = new StringBuilder();
                    string text = $"Recipe #{recipeIndex}: ingredients";
                    sb.Append(string.Format("{0, -26}", text));

                    foreach (var node in ingredientNodes)
                    {
                        text = node.InnerText.Contains(" de ") ?
                                        node.InnerText.Substring(node.InnerText.IndexOf("de") + 3) :
                                        node.InnerText.Contains(" d'") ?
                                        node.InnerText.Substring(node.InnerText.IndexOf("d'") + 2) :
                                        node.InnerText;

                        string ingredient = (text.StartsWith(" ") ? text.Remove(0, 1) : text).ToLower();
                        ingredient = (ingredient.StartsWith(" ") ? ingredient.Remove(0, 1) : ingredient).ToLower();

                        if (!ingredients.Any(i => i.Ingredient == ingredient))
                        {
                            ingredients.Add((ingredient, node.InnerText));
                        }
                        else
                        {
                            ingredient = "---";
                        }

                        text = $"| {string.Format("{0:0000}", ingredients.Count)} {ingredient}";

                        if (ingredientNodes.IndexOf(node) > 0 && ingredientNodes.IndexOf(node) % 6 == 0)
                        {
                            sb.Append(Environment.NewLine);
                            sb.Append(string.Format("{0, -26}", ""));
                        }

                        sb.Append(string.Format("{0, -35}", text));
                    }

                    Console.WriteLine(sb.ToString());

                    readStream.Close();
                }
            }

            recipeIndex++;
        }
    }
}
