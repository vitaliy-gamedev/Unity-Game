using UnityEngine;

public class InventoryBootstrap : MonoBehaviour
{
    private InventorySystem _inventorySystem;
    private InventoryUI _inventoryUI;

    private ItemDefinition _wood;
    private ItemDefinition _stone;
    private ItemDefinition _coal;
    private ItemDefinition _stick;
    private ItemDefinition _torch;
    private ItemDefinition _stoneBrick;

    private RecipeDefinition _stickRecipe;
    private RecipeDefinition _torchRecipe;
    private RecipeDefinition _stoneBrickRecipe;

    private void Awake()
    {
        CreateItems();
        CreateRecipes();

        _inventorySystem = gameObject.AddComponent<InventorySystem>();
        _inventorySystem.Initialize(new[]
        {
            _stickRecipe,
            _torchRecipe,
            _stoneBrickRecipe
        },
        new[]
        {
            _wood,
            _stone,
            _coal,
            _stick,
            _torch,
            _stoneBrick
        });

        _inventorySystem.AddStartingItem(_wood, 32);
        _inventorySystem.AddStartingItem(_stone, 16);
        _inventorySystem.AddStartingItem(_coal, 8);
        _inventorySystem.AddStartingItem(_stick, 4);

        _inventoryUI = gameObject.AddComponent<InventoryUI>();
        _inventoryUI.Initialize(_inventorySystem);
    }

    private void CreateItems()
    {
        _wood = CreateItem(
            "wood",
            "Дерево",
            64,
            new Color(0.55f, 0.28f, 0.08f));

        _stone = CreateItem(
            "stone",
            "Камень",
            64,
            new Color(0.55f, 0.55f, 0.55f));

        _coal = CreateItem(
            "coal",
            "Уголь",
            64,
            new Color(0.05f, 0.05f, 0.05f));

        _stick = CreateItem(
            "stick",
            "Палка",
            64,
            new Color(0.65f, 0.4f, 0.15f));

        _torch = CreateItem(
            "torch",
            "Факел",
            64,
            new Color(1f, 0.7f, 0.15f));

        _stoneBrick = CreateItem(
            "stone_brick",
            "Кирпич",
            64,
            new Color(0.35f, 0.35f, 0.4f));
    }

    private void CreateRecipes()
    {
        _stickRecipe = CreateRecipe(
            _stick,
            4,
            new RecipeIngredient(_wood, 2));

        _torchRecipe = CreateRecipe(
            _torch,
            4,
            new RecipeIngredient(_coal, 1),
            new RecipeIngredient(_stick, 1));

        _stoneBrickRecipe = CreateRecipe(
            _stoneBrick,
            1,
            new RecipeIngredient(_stone, 3));
    }

    private ItemDefinition CreateItem(
        string itemId,
        string displayName,
        int maxStackSize,
        Color iconColor)
    {
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.Initialize(
            itemId,
            displayName,
            maxStackSize,
            iconColor);

        return item;
    }

    private RecipeDefinition CreateRecipe(
        ItemDefinition result,
        int resultAmount,
        params RecipeIngredient[] ingredients)
    {
        RecipeDefinition recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        recipe.Initialize(
            result,
            resultAmount,
            ingredients);

        return recipe;
    }
}
