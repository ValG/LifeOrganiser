using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeOrganiser
{
    public class Recipe : IEquatable<Recipe>
    {
        public Recipe(string title, Bitmap image, Dictionary<Ingredient, Quantity> ingredients, List<(RecipesPanel.StringType StringType, string String)> preparation)
        {
            this.Ingredients = ingredients;
            this.Preparation = preparation;
            this.Image = image;
            this.Title = title;
        }

        public Dictionary<Ingredient, Quantity> Ingredients { get; private set; }
        public List<(RecipesPanel.StringType StringType, string String)> Preparation { get; private set; }
        public string Title { get; private set; }
        public Bitmap Image { get; private set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Recipe);
        }

        public bool Equals(Recipe other)
        {
            return this.Title == other?.Title;
        }
    }

    public class Ingredient
    {
        public Ingredient(string element, string extraInformation)
        {
            this.Element = element;
            this.ExtraInformation = extraInformation;
        }

        public string Element { get; protected set; }

        public string ExtraInformation { get; private set; }
    }

    public class NotAnIngredient : Ingredient
    {
        public NotAnIngredient (string title)
            : base (title, string.Empty)
        {
        }
    }

    public class Quantity
    {
        public Quantity(string dose, int value, Unity unity)
        {
            this.Dose = dose;
            this.Value = value;
            this.Unity = unity;
        }

        public string Dose { get; private set; }

        public int Value { get; private set; }

        public Unity Unity { get; private set; }
    }

    public enum Unity
    {
        Metric,
        Imperial
    }
}
