using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecipeIngredient
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private int _amount;

    public ItemDefinition Item => _item;
    public int Amount => _amount;

    public RecipeIngredient(ItemDefinition item, int amount)
    {
        _item = item;
        _amount = amount;
    }
}

[CreateAssetMenu(fileName = "RecipeDefinition", menuName = "Inventory/Recipe Definition")]
public class RecipeDefinition : ScriptableObject
{
    [SerializeField] private List<RecipeIngredient> _ingredients = new();
    [SerializeField] private ItemDefinition _result;
    [SerializeField] private int _resultAmount = 1;

    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;
    public ItemDefinition Result => _result;
    public int ResultAmount => _resultAmount;

    public void Initialize(ItemDefinition result, int resultAmount, params RecipeIngredient[] ingredients)
    {
        _result = result;
        _resultAmount = resultAmount;
        _ingredients = new List<RecipeIngredient>(ingredients);
    }
}