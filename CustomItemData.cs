using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ItemFilterLibrary;

public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, GameController gc, EKind kind, RectangleF clientRect = default) : base(queriedItem, gc)
    {
        Kind = kind;
        ClientRectangle = clientRect;
    }

    public RectangleF ClientRectangle { get; set; }
    public EKind Kind { get; }
}

public enum EKind
{
    QuestReward,
    Shop,
    RitualReward,
    PlayerInventory 
}