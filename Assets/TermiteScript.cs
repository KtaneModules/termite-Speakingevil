using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class TermiteScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public List<KMSelectable> buttons;
    public Renderer[] grid;
    public Renderer[] arrows;
    public Material[] io;
    public GameObject mite;

    private readonly bool[][] substrings = new bool[36][]
    {
        new bool[1]{ true}, new bool[3]{ false, false, true}, new bool[3]{ true, false, true}, new bool[2]{ true, false}, new bool[4]{ true, false, true, true}, new bool[4]{ false, true, false, false},
        new bool[4]{ false, true, true, false}, new bool[2]{ true, true}, new bool[4]{ false, true, false, false}, new bool[4]{ true, true, false, true}, new bool[3]{ false, true, true}, new bool[2]{ false, false},
        new bool[4]{ true, false, false, true}, new bool[3]{ true, false, true}, new bool[4]{ true, false, true, true}, new bool[3]{ true, true, false}, new bool[1]{ true}, new bool[4]{ false, false, true, false},
        new bool[1]{ false}, new bool[4]{ true, false, true, true}, new bool[2]{ true, false}, new bool[4]{ false, false, true, false}, new bool[3]{ false, true, true}, new bool[1]{ false},
        new bool[4]{ false, true, true, false}, new bool[2]{ false, true}, new bool[3]{ false, false, true}, new bool[4]{ true, false, false, true}, new bool[3]{ true, false, false}, new bool[3]{ false, true, false},
        new bool[2]{ true, true}, new bool[3]{ true, false, false}, new bool[2]{ false, true}, new bool[3]{ true, false, true}, new bool[2]{ false, false}, new bool[3]{ true, false, false}
    };
    private bool[][,] grids = new bool[2][,] { new bool[9,9], new bool[9,9]};
    private int[] posdir = new int[3];
    private bool[][] snippets = new bool[3][];
    private List<bool> instr = new List<bool> { };
    private string moves;
    private bool[] pressed = new bool[36];
    private int ans;
    private string solvemoves;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        module.OnActivate = Activate;
    }

    private void Activate()
    {
        snippets[0] = substrings["0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(info.GetSerialNumber().First())];
        snippets[1] = substrings[((info.GetBatteryCount() % 6) * 6) + (info.GetPortCount() % 6)];
        snippets[2] = substrings[(info.GetSerialNumberNumbers().Sum() + info.GetIndicators().Count()) % 36];
        instr = snippets[0].Concat(snippets[1]).Concat(snippets[2]).ToList();
        if (instr.Count() % 2 == 0)
            instr.RemoveAt(instr.Count() - 1);
        for(int i = 3; i < instr.Count(); i++)
        {
            if (instr[i] == instr[i - 1] && instr[i] != instr[i - 2] && instr[i] != instr[i - 3])
                instr[i] ^= true;
            else if (instr[i] != instr[i - 1] && instr[i] == instr[i - 2] && instr[i] != instr[i - 3])
                instr[i] ^= true;
            else if (i > 4 && instr[i] == instr[i - 3] && instr[i - 1] == instr[i - 4] && instr[i - 2] == instr[i - 5])
                instr[i] ^= true;
        }
        Debug.Log(string.Join("", instr.Select(x => x ? "R" : "L").ToArray()));
        StartCoroutine(Generate());
    }

    private IEnumerator Generate()
    {
        int[] steps = new int[2] { 0, 0};
        while (steps[0] < 30 || steps[0] > 149)
        {
            steps[0] = 0;
            moves = string.Empty;
            for (int i = 0; i < 81; i++)
            {
                grids[0][i / 9, i % 9] = Random.Range(0, 2) == 0;
                grids[1][i / 9, i % 9] = grids[0][i / 9, i % 9];
            }
            int r = Random.Range(0, 4);
            for (int i = 0; i < 2; i++)
                grids[i][r, 4] = true;
            posdir = new int[3] { 4, 4, 0 };
            while (posdir[0] >= 0 && posdir[0] < 9 && posdir[1] >= 0 && posdir[1] < 9 && steps[0] < 150)
            {
                if (steps[0] > 0 && grids[1][posdir[0], posdir[1]])
                {
                    if (instr[steps[1]])
                        posdir[2] = (posdir[2] + 1) % 4;
                    else
                        posdir[2] = (posdir[2] + 3) % 4;
                    steps[1] = (steps[1] + 1) % instr.Count;
                }
                grids[1][posdir[0], posdir[1]] ^= true;
                switch (posdir[2])
                {
                    case 0: posdir[0]--; moves += "U"; break;
                    case 1: posdir[1]++; moves += "R"; break;
                    case 2: posdir[0]++; moves += "D"; break;
                    case 3: posdir[1]--; moves += "L"; break;
                }
                steps[0]++;
            }
            yield return null;
            Debug.Log(steps[0] + ": " + moves);
        }
        for (int i = 0; i < 81; i++)
            grid[i].material = io[grids[0][i / 9, i % 9] ? 0 : 1];
        string[] rows = Enumerable.Range(0, 9).Select(z => string.Join(" ", Enumerable.Range(0, 9).Select(x => grids[0][z, x] ? "I" : "O").ToArray())).ToArray();
        Debug.LogFormat("[Termite #{0}] The initial grid:\n[Termite #{0}] {1}", moduleID, string.Join("\n[Termite #" + moduleID + "] ", rows));
        Debug.LogFormat("[Termite #{0}] The three sets of instructions are: {1}", moduleID, string.Join(", ", snippets.Select(x => string.Join("", x.Select(z => z ? "R" : "L").ToArray())).ToArray()));
        Debug.LogFormat("[Termite #{0}] The instruction tape is: {1}", moduleID, string.Join("", instr.Select(x => x ? "R" : "L").ToArray()));
        Debug.LogFormat("[Termite #{0}] The termite makes the moves: {1}", moduleID, moves);
        ans = posdir[2] * 9 + (posdir[2] % 2 == 0 ? posdir[1] : posdir[0]);
        Debug.LogFormat("[Termite #{0}] The termite escapes from the {1}{2} space along the {3} edge.", moduleID, (ans % 9) + 1, new string[] { "st", "nd", "rd", "th", "th", "th", "th", "th", "th"}[ans % 9], new string[] { "north", "east", "south", "west"}[posdir[2]]);
        foreach(KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate () { Press(b); return false; };
        }
    }

    private void Press(int b)
    {
        if (!moduleSolved && !pressed[b])
        {
            Audio.PlaySoundAtTransform("tick", buttons[b].transform);
            Debug.LogFormat("[Termite #{0}] Selected the {1}{2} space along the {3} edge.", moduleID, (b % 9) + 1, new string[] { "st", "nd", "rd", "th", "th", "th", "th", "th", "th" }[b % 9], new string[] { "north", "east", "south", "west" }[b / 9]);
            if (b == ans)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                moduleSolved = true;
                module.HandlePass();
                for (int i = 0; i < 36; i++)
                    arrows[i].enabled = false;
                arrows[b].enabled = true;
                arrows[b].material = io[0];
                StartCoroutine(Escape(moves));
            }
            else
            {
                module.HandleStrike();
                pressed[b] = true;
                arrows[b].enabled = true;
            }
        }
    }

    private IEnumerator Escape(string m)
    {
        int[] pos = new int[2] { 4, 4 };
        Vector3[] movedists = new Vector3[4] { new Vector3(0, 0, 0.11125f), new Vector3(0.1125f, 0, 0), new Vector3(0, 0, -0.11125f), new Vector3(-0.1125f, 0, 0)};
        int[] rots = new int[4] { 180, -90, 0, 90 };
        for (int i = 0; i < m.Length; i++)
        {
            int d = "URDL".IndexOf(m[i].ToString());
            grids[0][pos[0], pos[1]] ^= true;
            grid[(pos[0] * 9) + pos[1]].material = io[grids[0][pos[0], pos[1]] ? 0 : 1];
            switch (d)
            {
                case 0: pos[0]--; break;
                case 1: pos[1]++; break;
                case 2: pos[0]++; break;
                case 3: pos[1]--; break;
            }
            mite.transform.localPosition += movedists[d];
            mite.transform.localEulerAngles = new Vector3(0, rots[d], 0);
            yield return new WaitForSeconds(0.3f);
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} <NESW><#> [Selects arrow along specified edge at the # position from left to right/top to bottom]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if(command.Length != 2)
        {
            yield return "sendtochaterror!f Invalid command length.";
            yield break;
        }
        command = command.ToUpperInvariant();
        int r = "NESW".IndexOf(command[0].ToString());
        if(r < 0)
        {
            yield return "sendtochaterror!f Invalid edge.";
            yield break;
        }
        r *= 9;
        int s = 0;
        if (int.TryParse(command[1].ToString(), out s))
        {
            if (s > 0)
            {
                r += s - 1;
                yield return null;
                buttons[r].OnInteract();
            }
            else
            {
                yield return "sendtochaterror!f Number of spaces cannot be zero.";
                yield break;
            }
        }
        else
        {
            yield return "sendtochaterror!f Entered NaN spaces.";
            yield break;
        }
    }
}
