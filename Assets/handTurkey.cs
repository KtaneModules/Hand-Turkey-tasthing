using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class handTurkey : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Renderer[] feathers;
    public TextMesh colorblindText;
    public Color[] featherColors;
    public Color solveColor;

    private int[] featherColorIndices = new int[4];
    private int position;

    private static readonly string[] fingerNames = new[] { "pinky", "ring", "middle", "index" };
    private static readonly string[] colorNames = new[] { "brown", "red", "orange", "yellow", "green", "blue", "pink" };
    private static readonly string[] dishNames = new[] { "turkey", "cranberry sauce", "pumpkin pie", "lemonade", "becherovka", "quahogs", "benadryl" };
    private static readonly int[] colorTable = new[] { 0, 3, 5, 3, 0, 4, 6, 4, 6, 4, 3, 0, 0, 1, 1, 4, 0, 5, 1, 5, 3, 1, 2, 1, 6, 2, 2, 3, 0, 2, 3, 1, 2, 0, 4, 5, 5, 2, 5, 2, 6, 4, 1, 5, 3, 6, 6, 4, 6 };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    private void Start()
    {
        colorblindText.text = "";
        for (int i = 0; i < 4; i++)
        {
            var color = rnd.Range(0, 7);
            featherColorIndices[i] = color;
            feathers[i].material.color = featherColors[color];
            Debug.LogFormat("[Hand Turkey #{0}] The feather on the {1} finger is {2}. You grabbed {3}.", moduleId, fingerNames[i], colorNames[color], dishNames[color]);
            colorblindText.text += "NROYGBP"[color];
        }

        var startingRow = 0;
        var startingColumn = 0;
        if (Voltage() != -1d)
        {
            startingRow = (int)(Math.Floor(Voltage()) % 7);
            Debug.LogFormat("[Hand Turkey #{0}] A voltage meter is present. The starting row is {1}.", moduleId, startingRow);
        }
        else
        {
            startingRow = (colorNames[featherColorIndices[1]].Length + dishNames[featherColorIndices[3]].Length) % 7;
            Debug.LogFormat("[Hand Turkey #{0}] A voltage meter is not present. The number of letters in in {1} plus the number of letters in {2} modulo 7 is {3}. This is the starting row.", moduleId, colorNames[featherColorIndices[1]], dishNames[featherColorIndices[3]], startingRow);
        }
        var date = DateTime.Now.ToString("MMddyyyy");
        startingColumn = int.Parse(date) % 7;
        Debug.LogFormat("[Hand Turkey #{0}] The concatenated date is {1}, modulo 7 is {2}.", moduleId, date, startingColumn);
        position = startingRow * 7 + startingColumn;
        Debug.LogFormat("[Hand Turkey #{0}] Your starting position is {1}.", moduleId, Coordinate(position));

        // If you grabbed becherovka first...
        if (featherColorIndices[0] == 4)
        {
            Debug.LogFormat("[Hand Turkey #{0}] You are an alcoholic. Move down 4 spaces.", moduleId);
            for (int i = 0; i < 4; i++)
                position += position / 7 == 6 ? -42 : 7;
            Debug.LogFormat("[Hand Turkey #{0}] You are now at {1}.", moduleId, Coordinate(position));
        }
        // If you grabbed cranberry sauce, pumpkin pie, and lemonade...
        if (featherColorIndices.Contains(1) && featherColorIndices.Contains(2) && featherColorIndices.Contains(3)) 
        {
            Debug.LogFormat("[Hand Turkey #{0}] You are on a sugar high. Move up 3 spaces.", moduleId);
            for (int i = 0; i < 3; i++)
                position += position / 7 == 0 ? 42 : -7;
            Debug.LogFormat("[Hand Turkey #{0}] You are now at {1}.", moduleId, Coordinate(position));
        }
        // If you grabbed something more than once...
        foreach (int ix in Enumerable.Range(0, 7)) 
        {
            if (featherColorIndices.Count(x => x == ix) > 1)
            {
                var distances = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    var temPos = position;
                    var count = 0;
                    while (temPos == position || colorTable[temPos] != ix)
                    {
                        if (i == 0)
                            temPos += temPos % 7 == 0 ? 6 : -1;
                        else if (i == 1)
                            temPos += temPos / 7 == 0 ? 42 : -7;
                        else if (i == 2)
                            temPos += temPos % 7 == 6 ? -6 : 1;
                        else if (i == 3)
                            temPos += temPos / 7 == 6 ? -42 : 7;
                        count++;
                    }
                    distances[i] = count;
                }
                var direction = Array.IndexOf(distances, distances.Min());
                Debug.LogFormat("[Hand Turkey #{0}] You took {1} more than once. Move {2} 2 spaces.", moduleId, dishNames[ix], new[] { "left", "up", "right", "down" }[direction]);
                for (int i = 0; i < 2; i++)
                {
                    if (direction == 0)
                        position += position % 7 == 0 ? 6 : -1;
                    else if (direction == 1)
                        position += position / 7 == 0 ? 42 : -7;
                    else if (direction == 2)
                        position += position % 7 == 6 ? -6 : 1;
                    else if (direction == 3)
                        position += position / 7 == 6 ? -42 : 7;
                }
            }
        } 
        // If you grabbed four different items...
        if (featherColorIndices.Distinct().Count() == 4)
        {
        }
    }

    private static string Coordinate(int pos)
    {
        return "ABCDEFG"[pos % 7].ToString() + pos / 7;
    }

    private double Voltage()
    {
        if (bomb.QueryWidgets("volt", "").Count() != 0)
        {
            double TempVoltage = double.Parse(bomb.QueryWidgets("volt", "")[0].Substring(12).Replace("\"}", ""));
            return TempVoltage;
        }
        return -1d;
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} cycle # [Presses the arrow button # times] !{0} write [Presses the letter button] !{0} switch [Long presses the arrow button]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim().ToLowerInvariant();
        yield return null;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
