using System.Text;
using UnityEngine;

// Checks player names for swears, slurs and other nasty words.
// It sees through common tricks: CAPS, numbers for letters (n1gg4 / sh1t / @ss), stretched
// letters (fuuuuck) and symbols or spaces in between (f.u.c.k / f u c k).
// Two lists:
//   Anywhere  – blocked even inside a longer name ("xXshitXx").
//   WholeWord – only blocked as a word on its own, because they hide inside normal words
//               ("ass" in "class", "rape" in "grape", "cum" in "cucumber").
// The words are written out plainly so they're easy to check and add to – that's normal for a filter.
public static class NameFilter
{
    public const int MinLength = 2, MaxLength = 16;
    public const string DefaultName = "Scientist";

    static readonly string[] Anywhere =
    {
        // swears
        "fuck", "fuk", "fck", "shit", "cunt", "bitch", "biatch", "bastard", "asshole", "arsehole", "dickhead",
        "motherf", "wanker", "twat", "bollock", "jizz", "pussy", "penis", "vagina", "dildo", "porn", "blowjob",
        "handjob", "cumshot", "whore", "slut", "milf", "nude", "naked", "orgasm",
        // slurs and hate
        "nigg", "nigga", "niger", "negro", "faggot", "fagot", "retard", "tranny", "chink", "kike", "wetback",
        "beaner", "raghead", "towelhead", "gypsy", "nazi", "hitler", "heil", "kkk", "jihad", "isis",
        "holocaust", "genocide", "terrorist", "molest", "pedophil", "paedophil", "incest",
        // self-harm
        "killyourself", "suicide",
    };

    static readonly string[] WholeWord =
    {
        "ass", "arse", "asses", "dick", "dicks", "cock", "cocks", "cum", "tit", "tits", "titty", "boob", "boobs",
        "anal", "anus", "sex", "sexy", "horny", "boner", "wank", "hooker", "rape", "raped", "rapist",
        "fag", "fags", "spic", "spik", "coon", "coons", "gook", "dyke", "homo", "jap", "paki", "pedo", "paedo",
        "piss", "damn", "crap", "hoe", "hoes", "thot", "nonce", "kys",
    };

    // Throws away anything that isn't a letter, number, space, - or _ and caps the length.
    public static string Tidy(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        foreach (char ch in s)
        {
            if (sb.Length >= MaxLength) break;
            if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_') sb.Append(ch);
            else if (ch == ' ' && sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');   // no double spaces
        }
        return sb.ToString();
    }

    // True if the name is OK to use. 'reason' says what's wrong if it isn't.
    public static bool IsAllowed(string name, out string reason)
    {
        name = Tidy(name).Trim();
        if (name.Length < MinLength) { reason = "Too short"; return false; }
        if (!HasLetter(name)) { reason = "Needs at least one letter"; return false; }

        string lower = Leet(name.ToLowerInvariant());
        string joined = Squash(OnlyLetters(lower));           // "f u c k" / "f.u.c.k" -> "fuck"
        string joinedRaw = OnlyLetters(lower);
        foreach (string bad in Anywhere)
        {
            if (joinedRaw.Contains(bad)) { reason = "That name isn't allowed"; return false; }
            // Stretched letters ("fuuuck"). Skipped for very short words so "nig" doesn't block "Nightmare".
            string sq = Squash(bad);
            if (sq.Length >= 4 && joined.Contains(sq)) { reason = "That name isn't allowed"; return false; }
        }

        // Whole words: split on spaces, - and _ (and also the whole name run together, for "a s s").
        string[] words = lower.Split(' ', '-', '_');
        System.Array.Resize(ref words, words.Length + 1);
        words[words.Length - 1] = lower;
        foreach (string part in words)
        {
            string word = OnlyLetters(part);
            if (word.Length == 0) continue;
            foreach (string bad in WholeWord)
            {
                // Squashing only counts if they stretched it ("asss"), so "Bob" isn't read as "boob".
                bool stretched = word.Length > bad.Length && Squash(word) == Squash(bad);
                if (word == bad || stretched) { reason = "That name isn't allowed"; return false; }
            }
        }

        reason = "";
        return true;
    }

    public static bool IsAllowed(string name) => IsAllowed(name, out _);

    // A name from the other player: use it if it's fine, otherwise a safe default.
    public static string Safe(string name, string fallback)
    {
        name = Tidy(name).Trim();
        return IsAllowed(name) ? name : fallback;
    }

    static bool HasLetter(string s)
    {
        foreach (char ch in s) if (char.IsLetter(ch)) return true;
        return false;
    }

    // Numbers and symbols people use as letters.
    static string Leet(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s)
        {
            switch (ch)
            {
                case '0': sb.Append('o'); break;
                case '1': case '!': case '|': sb.Append('i'); break;
                case '3': sb.Append('e'); break;
                case '4': case '@': sb.Append('a'); break;
                case '5': case '$': sb.Append('s'); break;
                case '7': sb.Append('t'); break;
                case '8': sb.Append('b'); break;
                case '9': sb.Append('g'); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    static string OnlyLetters(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s) if (ch >= 'a' && ch <= 'z') sb.Append(ch);
        return sb.ToString();
    }

    // "fuuuuck" -> "fuck" (repeated letters squashed to one).
    static string Squash(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s) if (sb.Length == 0 || sb[sb.Length - 1] != ch) sb.Append(ch);
        return sb.ToString();
    }
}
