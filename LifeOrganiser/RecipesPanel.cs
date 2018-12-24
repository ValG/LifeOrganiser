using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace LifeOrganiser
{
    public partial class RecipesPanel : Form
    {
        public enum StringType
        {
            Title,
            Content
        }

        public RecipesPanel()
        {
            InitializeComponent();
            this.Hide();
            this.checkedListBox1.DisplayMember = "Title";
            //this.checkedListBox1.DataSource = devices;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) Console.WriteLine(file);
        }

        internal string GetWebContent(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return default;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string data = default;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    this.richTextBox1.Text = string.Empty;
                    this.richTextBox2.Text = string.Empty;

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

                    this.PrepareRecipe(htmlString);

                    //this.richTextBox1.Rtf = this.ToRtf(ingredientS, useLineNumber: false);
                    //this.richTextBox2.Rtf = this.ToRtf(preparationS, useLineNumber: true);

                    //if (!this.checkedListBox1.Items.Contains(title))
                    //{
                    //    this.checkedListBox1.Items.Add(title, true);
                    //}

                    readStream.Close();
                }
            }

            //.Split(Environment.NewLine.ToCharArray())

            return data;
        }


        private string ToRtf(List<(StringType StringType, string String)> tupleList, bool useLineNumber)
        {
            bool titleMet = false;
            int numberOfTitle = 0;
            StringBuilder stringBuilder = new StringBuilder();
            bool incrementLine = false;
            bool noTitle = !tupleList.Any(t => t.StringType == StringType.Title);

            foreach (var tuple in tupleList)
            {
                if (tuple.StringType == StringType.Title)
                {
                    stringBuilder.Append($"{(incrementLine ? "\\line" : string.Empty)} \\b {tuple.String}\\b0 \\line");

                    if (!incrementLine)
                    {
                        incrementLine = true;
                    }
                    else
                    {
                        titleMet = true;
                        numberOfTitle++;
                    }
                }
                else
                {
                    int decrement = 0;
                    string number = string.Empty;

                    if (titleMet)
                    {
                        decrement = numberOfTitle;
                    }
                    else if (noTitle)
                    {
                        decrement = -1;
                    }

                    if (useLineNumber)
                    {
                        number = $"  \\ul {(tupleList.IndexOf(tuple) - decrement).ToString()}.\\ul0 ";
                    }

                    stringBuilder.Append($"{number} {tuple.String} \\line");
                }
            }

            return @"{\rtf1\ansi\" + stringBuilder.ToString() + " }";
        }

        public Bitmap DownloadImage(string url, ImageFormat format)
        {
            Bitmap bitmap;

            using (WebClient client = new WebClient())
            {
                using (Stream stream = client.OpenRead(url))
                {
                    bitmap = new Bitmap(stream);
                }
            }

            return bitmap;
        }

        private Recipe PrepareRecipe(string webpage)
        {
            HtmlDocument document = new HtmlDocument();

            document.LoadHtml(webpage);

            HtmlNodeCollection ingredientsNode = document.DocumentNode.SelectNodes("//section[@id='ingredients']");
            HtmlNodeCollection preparationNode = document.DocumentNode.SelectNodes("//section[@id='preparation']");
            var title = document.DocumentNode.SelectNodes("//meta[@property='og:title']")?.Select(n => n.Attributes.Where(a => a.Name == "content").ElementAtOrDefault(0).Value).First();
            var imageUrl = document.DocumentNode.SelectNodes("//meta[@property='og:image']")?.Select(n => n.Attributes.Where(a => a.Name == "content").ElementAtOrDefault(0).Value).First();
            var original = this.DownloadImage(imageUrl, ImageFormat.Png);

            var ingredientsForm = ingredientsNode.FindFirst("form").ChildNodes.Where(cn => cn.Name == "ul" || cn.Name == "h3");
            var preparationSection = preparationNode.FindFirst("section").ChildNodes.Where(cn => cn.Name == "ol" || cn.Name == "h3");

            List<(StringType StringType, string String)> ingredients = new List<(StringType StringType, string String)>();
            List<(StringType StringType, string String)> preparation = new List<(StringType StringType, string String)>();

            foreach (var item in ingredientsForm)
            {
                if (item.Name == "h3")
                {
                    string temp = item.InnerText;
                    string tempWithoutSpace = string.Join(" ", temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
                    var realString = tempWithoutSpace.Split('\n', '\r').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                    foreach (string line in realString)
                    {
                        ingredients.Add((StringType.Title, line));
                    }
                }
                else if (item.Name == "ul")
                {
                    string temp = string.Join(string.Empty, item.ChildNodes.Where(cd => cd.Name == "li").Select(li => li.InnerText));
                    string tempWithoutSpace = string.Join(" ", temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
                    var realString = tempWithoutSpace.Split('\n', '\r').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                    foreach (string line in realString)
                    {
                        ingredients.Add((StringType.Content, line));
                    }
                }
            }

            foreach (var item in preparationSection)
            {
                if (item.Name == "h3")
                {
                    string temp = item.InnerText;
                    string tempWithoutSpace = string.Join(" ", temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
                    var realString = tempWithoutSpace.Split('\n', '\r').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                    foreach (string line in realString)
                    {
                        preparation.Add((StringType.Title, line));
                    }
                }
                else if (item.Name == "ol")
                {
                    string temp = string.Join(Environment.NewLine, item.ChildNodes.Where(cd => cd.Name == "li").Select(li => li.InnerText));
                    string tempWithoutSpace = string.Join(" ", temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
                    var realString = tempWithoutSpace.Split('\n', '\r').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                    foreach (string line in realString)
                    {
                        preparation.Add((StringType.Content, line));
                    }
                }
            }

            Dictionary<Ingredient, Quantity> ingredientsDict = new Dictionary<Ingredient, Quantity>(ingredients.Count);

            foreach (var ingredient in ingredients)
            {
                Ingredient _ingredient = null;
                Quantity _quantity = null;

                if (ingredient.StringType == StringType.Title)
                {
                    _ingredient = new NotAnIngredient(ingredient.String);
                }
                else
                {
                    var ingredientSplitted = ingredient.String.Split('\t').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    int value = int.Parse(string.Join(string.Empty, ingredientSplitted[0].Where(Char.IsDigit)));
                    string dose = string.Join(string.Empty, ingredientSplitted[0].Where(s => !Char.IsDigit(s) && !char.IsWhiteSpace(s)));
                    string element = string.Empty;
                    string extra = string.Empty;

                    if (ingredientSplitted.Count() > 1)
                    {
                        if (ingredientSplitted[1].Contains("de"))
                        {
                            var elementAndExtra = ingredientSplitted[1].Substring(ingredientSplitted[1].IndexOf("de") + 3).Split(' ');
                            element = new string(elementAndExtra.First(s => !string.IsNullOrWhiteSpace(s)).ToCharArray().Where(c => !Char.IsPunctuation(c)).ToArray());
                            extra = string.Join(" ", elementAndExtra.Where(s => !string.IsNullOrWhiteSpace(s) && s != element));
                        }
                        else
                        {
                            var elementAndExtra = ingredientSplitted[1].Split(' ');
                            element = new string(elementAndExtra.First(s => !string.IsNullOrWhiteSpace(s)).ToCharArray().Where(c => !Char.IsPunctuation(c)).ToArray());
                            extra = string.Join(" ", elementAndExtra.Where(s => !string.IsNullOrWhiteSpace(s) && s != element));
                        }
                    }

                    _ingredient = new Ingredient(element, extra);
                    _quantity = new Quantity(dose, value, Unity.Metric);
                }

                ingredientsDict.Add(_ingredient, _quantity);
            }

            Recipe recipe = new Recipe(title, original, ingredientsDict, preparation);

            this.pictureBox1.Image = recipe.Image;
            this.label1.Text = recipe.Title;
            this.richTextBox1.Rtf = this.ToRtf(ingredients, false);
            this.richTextBox2.Rtf = this.ToRtf(recipe.Preparation, true);

            if (!this.checkedListBox1.Items.Contains(recipe))
            {
                this.checkedListBox1.Items.Add(recipe, true);
            }

            return recipe;
        }

        private void checkedListBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            var list = sender as CheckedListBox;

            if (list?.SelectedItem is Recipe recipe)
            {
                this.pictureBox1.Image = recipe.Image;
                this.label1.Text = recipe.Title;
                this.richTextBox1.Text = "TODO BITCH";
                this.richTextBox2.Rtf = this.ToRtf(recipe.Preparation, true);
            }
        }

        private void buttonCreateList_Click(object sender, EventArgs e)
        {

        }

        private bool blockCheck = false;

        private void checkedListBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Left) & (e.X > 16))
            {
                this.blockCheck = true;
            }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (this.blockCheck)
            {
                this.blockCheck = false;
                e.NewValue = e.CurrentValue;
            }
        }
    }
}
