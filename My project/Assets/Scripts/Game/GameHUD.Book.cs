using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// THE RECIPE BOOK (press B, or SELECT / VIEW on a controller).
// Every recipe in the game, one page per station. The ones you've discovered show their picture, name and
// exact recipe. The ones you haven't are "???" - unless you've found a hint scroll for it this match.
// The HINTS page lists the hints you've collected this match.
public partial class GameHUD
{
    static readonly Station[] BookStations = { Station.Brew, Station.Workbench, Station.Enchant, Station.Terminal };
    static readonly string[] BookTabs = { "BREWING", "WORKBENCH", "ENCHANTING", "LAB TERMINAL", "HINTS" };
    static readonly Color BookGold = new Color(1f, 0.82f, 0.35f);
    static readonly Color BookPurple = new Color(0.7f, 0.45f, 1f);
    const int BookCols = 6;

    bool book;
    int bookTab, bookSel, bookScroll, bookFilter;      // filter: 0 all, 1 found, 2 not found
    float bookOpenedAt;

    public static bool BookOpen => I != null && I.book;

    void OpenBook(bool on)
    {
        book = on;
        bookSel = 0;
        bookScroll = 0;
        bookOpenedAt = Time.unscaledTime;
        if (on && station != null)
        {
            // open on the page for the station you're at
            for (int i = 0; i < BookStations.Length; i++) if (BookStations[i] == station.type) bookTab = i;
        }
        Sfx.Play(on ? "station_open" : "station_close", 0.4f);
    }

    static bool BookKey()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        return (k != null && k.bKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame);
    }

    // Called at the top of Update. Returns true when the book used the keys this frame.
    bool BookUpdate()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        if (!book)
        {
            if (paused || !BookKey()) return false;
            OpenBook(true);
            return true;
        }
        Player p = Player.Local;
        bool esc = (k != null && (k.escapeKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) || (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
        if (esc || BookKey() || p == null)
        {
            OpenBook(false);
            return true;
        }
        if (Time.unscaledTime - bookOpenedAt < 0.1f) return true;
        // switch pages: Q / E, or the shoulder buttons
        bool prevTab = (k != null && k.qKey.wasPressedThisFrame) || (g != null && g.leftShoulder.wasPressedThisFrame);
        bool nextTab = (k != null && k.eKey.wasPressedThisFrame) || (g != null && g.rightShoulder.wasPressedThisFrame);
        if (prevTab) SetBookTab((bookTab + BookTabs.Length - 1) % BookTabs.Length);
        if (nextTab) SetBookTab((bookTab + 1) % BookTabs.Length);
        // F (or Y): show all / found / not found
        if ((k != null && k.fKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame)) { bookFilter = (bookFilter + 1) % 3; bookSel = 0; bookScroll = 0; Sfx.Play("tick", 0.4f); }

        int count = bookTab < BookStations.Length ? BookList().Count : RecipeHints.All.Count;
        if (count > 0)
        {
            int cols = bookTab < BookStations.Length ? BookCols : 1;
            int was = bookSel;
            if (NavRight()) bookSel = Mathf.Min(count - 1, bookSel + 1);
            if (NavLeft()) bookSel = Mathf.Max(0, bookSel - 1);
            if (NavDown()) bookSel = Mathf.Min(count - 1, bookSel + cols);
            if (NavUp()) bookSel = Mathf.Max(0, bookSel - cols);
            if (bookSel != was) { Sfx.Play("tick", 0.25f); bookFollow = true; }
        }
        return true;
    }

    bool bookFollow;

    void SetBookTab(int t)
    {
        bookTab = t;
        bookSel = 0;
        bookScroll = 0;
        Sfx.Play("tick", 0.4f);
    }

    // The recipes on the current page, after the filter.
    List<Recipe> BookList()
    {
        var list = new List<Recipe>();
        if (bookTab >= BookStations.Length) return list;
        foreach (Recipe r in Items.For(BookStations[bookTab]))
        {
            bool known = Discoveries.Knows(r.id);
            if (bookFilter == 1 && !known) continue;
            if (bookFilter == 2 && known) continue;
            list.Add(r);
        }
        // found ones first, then the ones you have a hint for, then the rest (in recipe order)
        var sorted = new List<Recipe>(list.Count);
        foreach (Recipe r in list) if (Discoveries.Knows(r.id)) sorted.Add(r);
        foreach (Recipe r in list) if (!Discoveries.Knows(r.id) && RecipeHints.For(r.id) != null) sorted.Add(r);
        foreach (Recipe r in list) if (!Discoveries.Knows(r.id) && RecipeHints.For(r.id) == null) sorted.Add(r);
        return sorted;
    }

    static string KindName(ItemKind k)
    {
        switch (k)
        {
            case ItemKind.Weapon: return "weapon";
            case ItemKind.Potion: return "potion";
            case ItemKind.Throwable: return "bomb";
            case ItemKind.Armor: return "armour";
            case ItemKind.Enchant: return "enchant";
            default: return "upgrade";
        }
    }

    void DrawBook()
    {
        float w = Mathf.Min(1700f, W - 80f), h = 1000f;
        float x = (W - w) / 2f, y = 40f;
        Box(0f, 0f, W, 1080f, new Color(0f, 0f, 0f, 0.55f));
        Box(x, y, w, h, new Color(0.035f, 0.04f, 0.06f, 0.97f));
        Box(x, y, w, 6f, BookGold);
        Text(x + 30f, y + 14f, 600f, 56f, "RECIPE BOOK", 42, BookGold);

        // overall progress
        int all = 0, found = 0;
        foreach (Station s in BookStations)
            foreach (Recipe r in Items.For(s)) { all++; if (Discoveries.Knows(r.id)) found++; }
        float px = x + 420f, pw = w - 420f - 260f;
        Text(px, y + 14f, pw, 30f, "DISCOVERED  " + found + " / " + all + "   (" + Mathf.FloorToInt(100f * found / Mathf.Max(1, all)) + "%)", 18, Color.white);
        Bar(px, y + 48f, pw, 14f, found / (float)Mathf.Max(1, all), BookGold, new Color(0f, 0f, 0f, 0.6f));
        bool pad = Gamepad.current != null;
        Text(x, y + 14f, w - 30f, 56f, pad ? "SELECT / B  close" : "B / ESC  close", 16, Dim, TextAnchor.MiddleRight);

        // tabs
        float ty = y + 86f, tx = x + 30f;
        float tabW = (w - 60f - 8f * (BookTabs.Length - 1)) / BookTabs.Length;
        for (int i = 0; i < BookTabs.Length; i++)
        {
            string label = BookTabs[i];
            if (i < BookStations.Length)
            {
                List<Recipe> rs = Items.For(BookStations[i]);
                int f = 0;
                foreach (Recipe r in rs) if (Discoveries.Knows(r.id)) f++;
                label += "   " + f + "/" + rs.Count;
            }
            else label += "   " + RecipeHints.All.Count;
            bool on = bookTab == i;
            bool over = Hover(tx, ty, tabW, 50f);
            Box(tx, ty, tabW, 50f, on ? new Color(BookGold.r, BookGold.g, BookGold.b, 0.28f) : over ? new Color(1f, 1f, 1f, 0.1f) : new Color(1f, 1f, 1f, 0.05f));
            if (on) Box(tx, ty + 46f, tabW, 4f, BookGold);
            Text(tx, ty, tabW, 50f, label, 17, on ? Color.white : Dim, TextAnchor.MiddleCenter);
            if (Clicked(new Rect(tx, ty, tabW, 50f))) { SetBookTab(i); Event.current.Use(); }
            tx += tabW + 8f;
        }
        Text(x + 30f, ty + 54f, w, 26f, pad ? "LB / RB  change page   ·   Y  " + FilterName() : "Q / E  change page   ·   F  " + FilterName() + "   ·   mouse wheel to scroll", 14, Dim, TextAnchor.MiddleLeft, false);
        // filter button (mouse)
        float fbw = 230f, fbx = x + w - 30f - fbw;
        Box(fbx, ty + 56f, fbw, 26f, Hover(fbx, ty + 56f, fbw, 26f) ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));
        Text(fbx, ty + 56f, fbw, 26f, "SHOWING: " + (bookFilter == 0 ? "ALL" : bookFilter == 1 ? "FOUND" : "NOT FOUND"), 14, Color.white, TextAnchor.MiddleCenter);
        if (Clicked(new Rect(fbx, ty + 56f, fbw, 26f))) { bookFilter = (bookFilter + 1) % 3; bookSel = 0; bookScroll = 0; Sfx.Play("tick", 0.4f); Event.current.Use(); }

        float gy = ty + 92f, gh = y + h - 130f - gy;
        if (bookTab >= BookStations.Length) DrawBookHints(x, gy, w, gh);
        else DrawBookPage(x, gy, w, gh);
    }

    string FilterName() => "show " + (bookFilter == 0 ? "found only" : bookFilter == 1 ? "not found only" : "everything");

    void DrawBookPage(float x, float gy, float w, float gh)
    {
        List<Recipe> list = BookList();
        float footY = gy + gh + 10f;
        if (list.Count == 0)
        {
            Text(x + 30f, gy, w - 60f, 60f, bookFilter == 1 ? "You haven't found anything here yet. Try mixing materials at this station!" : "You've found everything on this page!", 20, Dim, TextAnchor.MiddleLeft, false);
            return;
        }
        float cw = (w - 60f - (BookCols - 1) * 10f) / BookCols, ch = 112f;
        int rows = Mathf.CeilToInt(list.Count / (float)BookCols);
        int visRows = Mathf.Max(1, Mathf.FloorToInt((gh + 10f) / (ch + 10f)));
        int maxScroll = Mathf.Max(0, rows - visRows);
        if (Event.current.type == EventType.ScrollWheel && R(x, gy, w, gh).Contains(Event.current.mousePosition))
        {
            bookScroll += Event.current.delta.y > 0f ? 1 : -1;
            Event.current.Use();
        }
        bookSel = Mathf.Clamp(bookSel, 0, list.Count - 1);
        if (bookFollow)
        {
            int selRow = bookSel / BookCols;
            if (selRow < bookScroll) bookScroll = selRow;
            if (selRow >= bookScroll + visRows) bookScroll = selRow - visRows + 1;
            bookFollow = false;
        }
        bookScroll = Mathf.Clamp(bookScroll, 0, maxScroll);

        Recipe detail = null;
        for (int i = bookScroll * BookCols; i < list.Count && i < (bookScroll + visRows) * BookCols; i++)
        {
            Recipe r = list[i];
            int row = i / BookCols - bookScroll, col = i % BookCols;
            float cx = x + 30f + col * (cw + 10f), cy = gy + row * (ch + 10f);
            bool known = Discoveries.Knows(r.id);
            RecipeHints.Hint hint = known ? null : RecipeHints.For(r.id);
            bool over = Hover(cx, cy, cw, ch);
            if (over && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f) bookSel = i;
            bool sel = bookSel == i;
            if (sel) detail = r;
            if (known)
            {
                Box(cx, cy, cw, ch, sel ? new Color(r.color.r, r.color.g, r.color.b, 0.25f) : new Color(1f, 1f, 1f, 0.06f));
                Box(cx, cy, 4f, ch, r.color);
                Icon(cx + 10f, cy + 8f, 56f, Items.Icon(r.icon), r.color);
                Text(cx + 72f, cy + 6f, cw - 78f, 26f, r.name, 15, Color.white, TextAnchor.MiddleLeft, true);
                Text(cx + 72f, cy + 32f, cw - 78f, 22f, KindName(r.kind) + (r.makes > 1 ? "  ·  makes " + r.makes : ""), 12, Dim, TextAnchor.MiddleLeft, false);
                // the recipe: material icons with counts
                float mx = cx + 10f;
                if (r.hideRecipe) Text(cx + 10f, cy + 72f, cw - 20f, 28f, "recipe: ? ? ?", 14, new Color(1f, 0.3f, 0.9f));
                else for (int m = 0; m < Items.MatCount; m++)
                {
                    if (r.cost[m] <= 0) continue;
                    if (mx > cx + cw - 50f) { Text(mx, cy + 70f, 30f, 34f, "...", 14, Dim); break; }
                    Icon(mx, cy + 72f, 28f, Items.MatIcon(m), Items.MatColors[m]);
                    Text(mx + 28f, cy + 72f, 26f, 28f, "x" + r.cost[m], 13, Color.white);
                    mx += 56f;
                }
            }
            else if (hint != null)
            {
                Box(cx, cy, cw, ch, sel ? new Color(BookGold.r, BookGold.g, BookGold.b, 0.22f) : new Color(BookGold.r, BookGold.g, BookGold.b, 0.08f));
                Box(cx, cy, 4f, ch, BookGold);
                Text(cx + 14f, cy + 6f, cw - 20f, 30f, "???   HINT", 18, BookGold);
                Text(cx + 14f, cy + 36f, cw - 24f, ch - 40f, hint.text, 12, Color.white, TextAnchor.UpperLeft, false, true);
            }
            else
            {
                Box(cx, cy, cw, ch, sel ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.025f));
                Text(cx, cy + 10f, cw, 60f, "???", 34, new Color(1f, 1f, 1f, 0.22f), TextAnchor.MiddleCenter);
                Text(cx, cy + 70f, cw, 30f, "a secret " + KindName(r.kind), 13, new Color(1f, 1f, 1f, 0.3f), TextAnchor.MiddleCenter, false);
            }
            if (sel) { Box(cx, cy, cw, 2f, Color.white); Box(cx, cy + ch - 2f, cw, 2f, Color.white); }
        }
        // scroll bar
        if (maxScroll > 0)
        {
            float sbx = x + w - 18f;
            Box(sbx, gy, 6f, gh, new Color(1f, 1f, 1f, 0.06f));
            float k = visRows / (float)rows;
            Box(sbx, gy + gh * (bookScroll / (float)rows), 6f, Mathf.Max(20f, gh * k), new Color(1f, 1f, 1f, 0.35f));
        }

        // details of the selected recipe
        Box(x + 30f, footY, w - 60f, 100f, new Color(1f, 1f, 1f, 0.04f));
        if (detail == null) return;
        if (Discoveries.Knows(detail.id))
        {
            Icon(x + 44f, footY + 12f, 76f, Items.Icon(detail.icon), detail.color);
            Text(x + 136f, footY + 8f, w - 200f, 34f, detail.name, 24, detail.color);
            Text(x + 136f, footY + 42f, w - 200f, 54f, detail.desc, 16, Color.white, TextAnchor.UpperLeft, false, true);
        }
        else
        {
            RecipeHints.Hint hint = RecipeHints.For(detail.id);
            Text(x + 50f, footY + 8f, w - 100f, 34f, hint != null ? "HINT" : "NOT DISCOVERED YET", 22, hint != null ? BookGold : Dim);
            Text(x + 50f, footY + 42f, w - 100f, 54f, hint != null ? hint.text : "A secret " + KindName(detail.kind) + ". Mix materials at the " + LabStation.Names[(int)detail.station] + " to find it - or look for glowing hint scrolls around the map.", 16, Color.white, TextAnchor.UpperLeft, false, true);
        }
    }

    void DrawBookHints(float x, float gy, float w, float gh)
    {
        IReadOnlyList<RecipeHints.Hint> hs = RecipeHints.All;
        if (hs.Count == 0)
        {
            Text(x + 30f, gy, w - 60f, 120f, "No hints yet this match.\nGlowing gold scrolls are hidden around the map - in different places every game. Press E on one to read a hint for a recipe you haven't found.", 20, Dim, TextAnchor.UpperLeft, false, true);
            return;
        }
        float rh = 64f;
        int vis = Mathf.Max(1, Mathf.FloorToInt(gh / (rh + 8f)));
        int maxScroll = Mathf.Max(0, hs.Count - vis);
        if (Event.current.type == EventType.ScrollWheel && R(x, gy, w, gh).Contains(Event.current.mousePosition))
        {
            bookScroll += Event.current.delta.y > 0f ? 1 : -1;
            Event.current.Use();
        }
        bookSel = Mathf.Clamp(bookSel, 0, hs.Count - 1);
        if (bookFollow)
        {
            if (bookSel < bookScroll) bookScroll = bookSel;
            if (bookSel >= bookScroll + vis) bookScroll = bookSel - vis + 1;
            bookFollow = false;
        }
        bookScroll = Mathf.Clamp(bookScroll, 0, maxScroll);
        for (int i = bookScroll; i < hs.Count && i < bookScroll + vis; i++)
        {
            RecipeHints.Hint h = hs[i];
            float ry = gy + (i - bookScroll) * (rh + 8f);
            bool done = Discoveries.Knows(h.recipe.id);
            bool over = Hover(x + 30f, ry, w - 60f, rh);
            if (over && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f) bookSel = i;
            Box(x + 30f, ry, w - 60f, rh, bookSel == i ? new Color(BookGold.r, BookGold.g, BookGold.b, 0.18f) : new Color(1f, 1f, 1f, 0.05f));
            Box(x + 30f, ry, 4f, rh, done ? new Color(0.45f, 1f, 0.35f) : BookGold);
            if (done)
            {
                Icon(x + 44f, ry + 6f, 52f, Items.Icon(h.recipe.icon), h.recipe.color);
                Text(x + 108f, ry, w - 200f, rh, "SOLVED!  " + h.recipe.name + "   -   " + h.text, 16, new Color(0.6f, 1f, 0.5f), TextAnchor.MiddleLeft, false, true);
            }
            else Text(x + 50f, ry, w - 120f, rh, h.text, 18, Color.white, TextAnchor.MiddleLeft, false, true);
        }
        Text(x + 30f, gy + gh + 20f, w - 60f, 60f, "Hints only last for this match. Next game the scrolls move and the hints change!", 16, Dim, TextAnchor.UpperLeft, false, true);
    }
}
