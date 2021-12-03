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

    public KMSelectable[] buttons;
    public Renderer[] feathers;
    public TextMesh[] buttonTexts;
    public TextMesh turkeyText;
    public TextMesh colorblindText;
    public Color[] featherColors;
    public Color solveColor;

    private int[] featherColorIndices = new int[4];
    private int calculatedNumber;
    private string solution;
    private int currentLetter;
    private int enteringStage;
    private List<string> otherModules = new List<string>();

    private static readonly string[] fingerNames = new[] { "pinky", "ring", "middle", "index" };
    private static readonly string[] colorNames = new[] { "red", "green", "cyan", "magenta" };
    private static readonly string[] colorTable = new[] { "crimson", "sanguine", "carmine", "scarlet", "lime", "forest", "verdant", "olive", "turquoise", "teal", "aquamarine", "azure", "purple", "violet", "lavender", "mauve" };
    private static readonly string[] gratefulTable = new[] { "family", "friends", "food", "house", "medicine", "doctors", "firemen", "animals", "music", "games", "football", "modules", "weather", "nature", "quaaludes", "Latinas" };
    private bool longPress;
    private int offset = 1;
    private Coroutine counting;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate () { PressButton(button); return false; };
            button.OnInteractEnded += delegate () { ReleaseButton(button); };
        }
        colorblindText.gameObject.SetActive(GetComponent<KMColorblindMode>().ColorblindModeActive);
    }

    private void Start()
    {
        otherModules = bomb.GetSolvableModuleNames();
        otherModules.Remove("Hand Turkey");
        otherModules = otherModules.Select(x => x.ToUpperInvariant()).ToList();

        featherColorIndices = new int[4].Select(x => rnd.Range(0, 4)).ToArray();
        colorblindText.text = featherColorIndices.Select(x => "RGCM"[x]).Join("");
        for (int i = 0; i < 4; i++)
        {
            feathers[i].material.color = featherColors[featherColorIndices[i]];
            var word = colorTable[featherColorIndices[i] * 4 + i];
            var match = word.ToUpperInvariant().Any(ch => otherModules.Any(str => str[0] == ch));
            Debug.LogFormat("[Hand Turkey #{0}] {1} finger: The feather is {2}, and the synonym used is {3}. Matching modules {4}found; add a {5} to the binary number.", moduleId, fingerNames[i], colorNames[featherColorIndices[i]], word, match ? "" : "not ", match ? "1" : "0");
            if (match)
                calculatedNumber += 1 << (3 - i);
        }
        Debug.LogFormat("[Hand Turkey #{0}] The final calculated binary number is {1} ({2}). You are grateful for {4}{3}.", moduleId, Convert.ToString(calculatedNumber, 2).PadLeft(4, '0'), calculatedNumber, gratefulTable[calculatedNumber], calculatedNumber < 4 ? "your " : ""); ;
        solution = gratefulTable[calculatedNumber].ToUpperInvariant();
    }

    private void PressButton(KMSelectable button)
    {
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved)
            return;
        if (Array.IndexOf(buttons, button) == 0)
            counting = StartCoroutine(CountUp());
        else
        {
            if (solution[enteringStage].ToString() == buttonTexts[1].text)
            {
                turkeyText.text += buttonTexts[1].text;
                enteringStage++;
                if (turkeyText.text == solution)
                {
                    Debug.LogFormat("[Hand Turkey #{0}] Finished entering the word. Module solved!", moduleId);
                    module.HandlePass();
                    moduleSolved = true;
                    audio.PlaySoundAtTransform("solve", transform);
                    colorblindText.text = "";
                    foreach (Renderer feather in feathers)
                        feather.material.color = solveColor;
                    foreach (TextMesh t in buttonTexts)
                        t.text = "-";
                }
            }
            else
            {
                Debug.LogFormat("[Hand Turkey #{0}] Struck because you tried to enter a wrong letter!", moduleId);
                module.HandleStrike();
            }
        }
    }

    private void ReleaseButton(KMSelectable button)
    {
        if (Array.IndexOf(buttons, button) == 1 || moduleSolved)
            return;
        if (counting != null)
        {
            StopCoroutine(counting);
            counting = null;
        }
        if (longPress)
        {
            offset *= -1;
            buttonTexts[0].transform.localEulerAngles = offset == 1 ? new Vector3(180f, 0f, 0f) : new Vector3(180f, 0f, 180f);
            buttonTexts[0].transform.localPosition = offset == 1 ? new Vector3(.00139f, .00151f, .0056f) : new Vector3(-.0008f, .0003f, .0056f);
        }
        else
        {
            currentLetter = (currentLetter + 26 + offset) % 26;
            buttonTexts[1].text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[currentLetter].ToString();
        }
        longPress = false;
    }

    private IEnumerator CountUp()
    {
        yield return new WaitForSeconds(1f);
        longPress = true;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, buttons[0].transform);
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} cycle # [Presses the arrow button # times] !{0} write [Presses the letter button] !{0} switch [Long presses the arrow button]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim().ToLowerInvariant();
        if (input.StartsWith("cycle "))
        {
            yield return null;
            var rest = input.Substring(6);
            var x = 0;
            if (int.TryParse(input.Substring(6), out x))
            {
                if (x <= 0)
                {
                    yield return string.Format("sendtochaterror {0} is not a valid number.", rest);
                    yield break;
                }
                else
                {
                    for (int i = 0; i < x; i++)
                    {
                        yield return new WaitForSeconds(.1f);
                        buttons[0].OnInteract();
                        yield return new WaitForSeconds(.1f);
                        buttons[0].OnInteractEnded();
                    }
                }
            }
            else
            {
                yield return string.Format("sendtochaterror {0} is not a valid number.", rest);
                yield break;
            }
        }
        else if (input == "write")
        {
            yield return null;
            buttons[1].OnInteract();
        }
        else if (input == "switch" || input == "flip" || input == "reverse")
        {
            yield return null;
            buttons[0].OnInteract();
            yield return new WaitForSeconds(1.01f);
            buttons[0].OnInteractEnded();
        }
        else
            yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (buttonTexts[1].text != solution[enteringStage].ToString())
            {
                buttons[0].OnInteract();
                yield return null;
                buttons[0].OnInteractEnded();
                yield return new WaitForSeconds(.1f);
            }
            buttons[1].OnInteract();
        }
    }
}
