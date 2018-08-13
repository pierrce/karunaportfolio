using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class PlayerInventory
{
    public const int INVENTORY_ROWS = 5;
    public const int INVENTORY_COLS = 5;
    public const int INVENTORY_QUICK_COUNT = 3;

    /* The list of items in the inventory */
    public Item[] items;
    public bool[] items_valid;

    /* The quick slot items */
    public Item[] q;
    public bool[] q_valid;

    /* The amount of items in the inventory. Must be less than or equal to capacity. */
    public int used_space;

    /* The amonunt of currency the user currently has */
    public long gold;

    /**
     * Create a new player inventory with no items in it.
     */
    public PlayerInventory()
    {
        /* Set all items to null */
        items = new Item[INVENTORY_ROWS * INVENTORY_COLS];
        items_valid = new bool[INVENTORY_ROWS * INVENTORY_COLS];
        for (int i = 0; i < INVENTORY_ROWS * INVENTORY_COLS; i++)
        {
            items[i] = Item.null_item;
            items_valid[i] = false;
        }

        /* Set all quick slots to null */
        q = new Item[INVENTORY_QUICK_COUNT];
        q_valid = new bool[INVENTORY_QUICK_COUNT];
        for (int i = 0; i < INVENTORY_QUICK_COUNT; i++)
        {
            q[i] = Item.null_item;
            q_valid[i] = false;
        }

        /* No slots are in use */
        used_space = 0;

        gold = 0L;
    }

    /**
     * Copy constructor. This is potentially slow and should probably only
     * be used when checking to see if a recipe can be made from the current
     * inventory.
     */
    public PlayerInventory(PlayerInventory inventory)
    {
        items = new Item[INVENTORY_ROWS * INVENTORY_COLS];
        items_valid = new bool[INVENTORY_ROWS * INVENTORY_COLS];
        used_space = inventory.used_space;
        gold = 0L;

        for (int row = 0;row < INVENTORY_ROWS;row++)
        {
            for(int col = 0;col < INVENTORY_COLS;col++)
            {
                int index = (row * INVENTORY_COLS) + col;

                items_valid[index] = inventory.items_valid[index];

                if (items_valid[index])
                    items[index] = new Item(inventory.items[index]);
                else items[index] = Item.null_item;
            }
        }
    }

    /**
     * Make sure that one of our developers hasn't messed up
     * the internal structures.
     */
    public void Verify()
    {
        for (int i = 0; i < items.Length; i++)
        {
            /* Did someone mess up our items list? */
            if (items[i] == null)
            {
                items[i] = Item.null_item;
                items_valid[i] = false;
                continue;
            }

            /* Is this a possible duplicate of the null item? */
            if (items[i].item_name.Equals("null"))
            {
                items[i] = Item.null_item;
                items_valid[i] = false;
                continue;
            }

            /* This is a valid item */
            items_valid[i] = true;
        }
    }

    public void Apply(PlayerInventory inventory)
    {
        /* Verify everything for our sanity */
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new Item(inventory.items[i]);
            items_valid[i] = inventory.items_valid[i];
        }

        this.used_space = inventory.used_space;
        this.gold = inventory.gold;
    }

    /**
     * Set the amount of coins in the player's inventory.
     */
    public void SetGold(long gold)
    {
        this.gold = gold;
    }

    /**
     * Swaps the locations of two objects in the inventory.
     */
    public void Swap(int r1, int c1, int r2, int c2)
    {
        if (r1 < 0 || c1 < 0 || r1 >= INVENTORY_ROWS || c1 >= INVENTORY_COLS)
            throw new System.Exception("Row or column out of range!");
        if (r2 < 0 || c2 < 0 || r2 >= INVENTORY_ROWS || c2 >= INVENTORY_COLS)
            throw new System.Exception("Row or column out of range!");

        /* Get the indexes of both of the items */
        int i1 = (r1 * INVENTORY_COLS) + c1;
        int i2 = (r2 * INVENTORY_COLS) + c2;

        Item item1 = items[i1];
        Item item2 = items[i2];
        bool v1 = items_valid[i1];
        bool v2 = items_valid[i2];

        items[i1] = item2;
        items[i2] = item1;
        items_valid[i1] = v2;
        items_valid[i2] = v1;
    }

    /**
     * Returns the item at row r and column c in the inventory. Returns
     * null if there is no item at the row and column
     */
    public Item Get(int r, int c)
    {
        if (r < 0 || c < 0 || r >= INVENTORY_ROWS || c >= INVENTORY_COLS)
            throw new System.Exception("Row or column out of range!");

        /* Get the index of the item */
        int index = (r * INVENTORY_COLS) + c;

        /* Return the item */
        if (items_valid[index])
            return items[(r * INVENTORY_COLS) + c];
        else return null;
    }

    /**
     * Get the position of an item in the inventory. If the item
     * exists in the inventory, true is returned. Otherwise false
     * is returned. The row and column of the inventory are returned
     * through parameters.
     */
    public bool GetPosition(Item item, out int row, out int col)
    {
        for(int r = 0;r < INVENTORY_ROWS;r++)
        {
            for(int c = 0;c < INVENTORY_COLS;c++)
            {
                /* Get this item */
                Item i = Get(r, c);
                if (!Item.IsNull(i))
                    continue;

                /* Are these items equal? */
                if(i.Equals(item))
                {
                    /* These items are equal */
                    row = r;
                    col = c;
                    return true;
                }
            }
        }

        /* We failed to find the item */
        row = -1;
        col = -1;
        return false;
    }

    /**
     * Set a specific item in the inventory.
     */
    public void Set(int r, int c, Item i)
    {
        if (r < 0 || c < 0 || r >= INVENTORY_ROWS || c >= INVENTORY_COLS)
            throw new System.Exception("Row or column out of range!");

        /* Get the index for this row,column pair */
        int index = (r * INVENTORY_COLS) + c;

        /* Update used space */
        if (!items_valid[index] && i != null)
            used_space++;
        else if (items_valid[index] && i == null)
            used_space--;

        /* Update the inventory, make a copy if needed */
        if (i == null)
            items[index] = Item.null_item;
        else items[index] = new Item(i);

        /* Update the valid array */
        items_valid[index] = i != null;
    }

    /**
     * Get the capacity of the inventory.
     */
    public int GetCapacity()
    {
        return INVENTORY_ROWS * INVENTORY_COLS;
    }

    /**
     * Returns whether or not the inventory is full.
     */
    public bool IsFull()
    {
        return used_space == GetCapacity();
    }

    /**
     * Returns whether or not the space is empty.
     */
    public bool IsEmpty()
    {
        return used_space == 0;
    }

    /**
     * Internal function to remove an object at an index. Returns
     * the removed item.
     */
    internal Item InternalRemove(int index)
    {
        /* Get the item */
        Item item = items[index];

        /* Update used space if needed */
        if (item != null)
            used_space--;

        /* Remove the item */
        items[index] = Item.null_item;

        /* This item is no longer valid */
        items_valid[index] = false;

        /* Return the removed item */
        return item;
    }

    /**
     * Remove an item at the given row and column. If there is
     * no item at the given row and column, null is returned.
     */
    public Item Remove(int row, int col)
    {
        if (row < 0 || col < 0 || row >= INVENTORY_ROWS || col >= INVENTORY_COLS)
            throw new System.Exception("Index out of range!");

        /* Get the index of the item */
        int index = (INVENTORY_COLS * row) + col;

        return InternalRemove(index);
    }

    /**
     * Remove the item from the inventory. Returns the position of
     * the item as the parameters.
     */
    public bool Remove(Item item, out int row, out int col)
    {
        int r, c;

        row = -1;
        col = -1;

        /* Get the position of the given item */
        if (GetPosition(item, out r, out c))
        {
            /* Remove the item at the given position */
            Item removed = Remove(row, col);
            if (removed == null)
                return false;

            /* Make sure we removed the right thing */
            if (!removed.Equals(item))
                return false;

            /* Update the position */
            row = r;
            col = c;

            /* The item was removed successfully */
            return true;
        }

        /* We couldn't remove the item */
        return false;
    }

    /**
     * Removes the given item from the inventory. Returns true if the
     * item could be removed from the inventory, false otherwise.
     */
    public bool Remove(Item item)
    {
        int row, col;
        return Remove(item, out row, out col);
    }

    /**
     * Remove the given amount of items from this inventory. Returns
     * the amount of that item that was removed.
     */
    public int Remove(Item i, int count)
    {
        if (i == null)
            return 0;

        int removed = 0;

        for (int x = 0; x < items.Length; x++)
        {
            if (items[x].Equals(i))
            {
                InternalRemove(x);
                removed++;
            }
        }

        return removed;
    }

    /**
     * Remove all occurances of the given item from the inventory. Returns
     * the amount of items removed from the inventory
     */
    public int RemoveAll(Item i)
    {
        int removed = 0;

        for (int x = 0; x < items.Length; x++)
        {
            if (items[x].Equals(i))
            {
                InternalRemove(x);
                removed++;
            }
        }

        return removed;
    }

    /**
     * Remove the list of items from this inventory. This is useful for
     * removeing a recipe of items from an inventory. Returns true on
     * success, false otherwise.
     */
    public bool RemoveItems(Item[] items)
    {
        foreach (Item i in items)
        {
            if (!Remove(i))
                return false;
        }

        return true;
    }

    /**
     * Add an item to the next available slot in the inventory. Returns true
     * if the item was added or false if the item was not added.
     */
    public bool Add(Item i, out int row, out int col)
    {
        /* Make sure the new item is not null */
        if (i == null)
        {
            row = -1;
            col = -1;
            return false;
        }

        /* Do we have room? */
        if (IsFull())
        {
            row = -1;
            col = -1;
            return false;
        }

        /* Add the item */
        for (int x = 0; x < items.Length; x++)
        {
            if (!items_valid[x])
            {
                used_space++;
                items[x] = new Item(i);
                items_valid[x] = true;

                /* Return the row and the column for this item */
                row = x / INVENTORY_COLS;
                col = x % INVENTORY_COLS;

                /* We added the item */
                return true;
            }
        }

        /* The item could not be added */
        row = -1;
        col = -1;
        return false;
    }

    /**
     * Add an item to the next available slot in the inventory. Returns true
     * if the item was added or false if the item was not added.
     */
    public bool Add(Item i)
    {
        int row, col;
        return Add(i, out row, out col);
    }

    /**
     * Add copies of the given item an amount of times to the inventory. Returns
     * the amount of items added.
     */
    public int Add(Item i, int amount)
    {
        if (i == null)
            return 0;

        for (int x = 0; x < amount; x++)
        {
            if (!Add(i))
                return x;
        }

        return amount;
    }

    /**
     * Returns a reference to the given quick item on the player's waist.
     */
    public Item GetQuickItem(int index)
    {
        if (index < 0 || index >= INVENTORY_QUICK_COUNT)
            throw new System.Exception("index out of range!");

        if (q_valid[index])
            return q[index];
        return null;
    }

    /**
     * Replace the given item in the player's quick item slots.
     */
    public void SetQuickItem(int index, Item i)
    {
        if (index < 0 || index >= INVENTORY_QUICK_COUNT)
            throw new System.Exception("index out of range!");

        /* Is the item valid? */
        if (i == null)
        {
            /* The item is null */
            q[index] = Item.null_item;
            q_valid[index] = false;
        } else
        {
            /* The item is valid */
            q[index] = new Item(i);
            q_valid[index] = true;
        }
    }

    /**
     * Remove a quick item. Returns the item that was removed. If no
     * item could be removed, then this returns null.
     */
    public Item RemoveQuickItem(int index)
    {
        if (index < 0 || index >= INVENTORY_QUICK_COUNT)
            throw new System.Exception("index out of range!");

        /* Get the item from the quick slots */
        Item i = q[index];

        /* Update the tables */
        q[index] = Item.null_item;
        q_valid[index] = false;

        /* Return the item */
        return i;
    }

    /**
     * Clears both the regular inventory and the quick slots.
     */
    public void ClearEntireInventory()
    {
        for (int x = 0; x < items.Length; x++)
        {
            items[x] = Item.null_item;
            items_valid[x] = false;
        }

        for (int x = 0; x < q.Length; x++)
        {
            q[x] = Item.null_item;
            q_valid[x] = false;
        }

        used_space = 0;
    }

    /**
     * Clears the regular inventory.
     */
    public void ClearInventory()
    {
        for (int x = 0; x < items.Length; x++)
        {
            items[x] = Item.null_item;
            items_valid[x] = false;
        }

        used_space = 0;
    }

    /**
     * Clears both the regular inventory and the quick slots.
     */
    public void ClearQuickSlots()
    {
        for (int x = 0; x < q.Length; x++)
        {
            q[x] = Item.null_item;
            q_valid[x] = false;
        }
    }

    public void Dump()
    {
        Debug.Log("Item count: " + used_space);
        for (int x = 0; x < items.Length; x++)
        {
            int r = (x / INVENTORY_COLS);
            int c = x % INVENTORY_COLS;

            if (!items_valid[x]) continue;

            Debug.Log("ROW: " + r + " COL: " + c + " ITEM: " + items[x]);
        }
    }

    /* Returns the total number of a specific item in an inventory */
    public int CountItemsById(string item_id)
    {
        int totalNum = 0;

        for(int i = 0; i<INVENTORY_ROWS; i++)
        {
            for(int j = 0; j<INVENTORY_COLS; j++)
            {
                Item item = Get(i, j);
                if (item == null)
                    continue;

                if (item.item_id == item_id)
                    totalNum++;
            }
        }

        return totalNum;
    }
}
