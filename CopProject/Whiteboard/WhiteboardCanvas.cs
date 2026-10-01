using System.Collections.ObjectModel;
using Whiteboard.Items;

namespace Whiteboard;

public class WhiteboardCanvas
{
    public ObservableCollection<WhiteboardItem> Items { get; } = new();

    public void AddItem(WhiteboardItem item)
    {
        Items.Add(item);
    }

    public void RemoveItem(WhiteboardItem item)
    {
        Items.Remove(item);
    }
}
