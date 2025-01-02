﻿using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class BlackButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public Material[] BandColors;
    public MeshRenderer[] Resistor1Bands, Resistor2Bands, Resistor3Bands;
    public TextMesh CapacitorText;
    public TextMesh SecondsText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private double _minTime, _maxTime;
    private float? _lastHeldTime = null;
    private int capac;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;

        tryAgain:
        int[] resistences = new int[] { Rnd.Range(0, 1000), Rnd.Range(0, 1000), Rnd.Range(0, 1000) };
        int[] exponents = new int[] { Rnd.Range(0, 6), Rnd.Range(0, 6), Rnd.Range(0, 6) };
        double resistence = 1 / (1 / (resistences[1] * Math.Pow(10, exponents[1])) + 1 / (resistences[2] * Math.Pow(10, exponents[2])) + 1 / (resistences[0] * Math.Pow(10, exponents[0])));

        float targetTime = Rnd.Range(10f, 35f);
        double capacitance = targetTime / resistence;
        if (capacitance >= 0.001d)
            goto tryAgain;
        if (capacitance <= 0.000000001d)
            goto tryAgain;

        string capText = "";

        if (capacitance >= 0.000001d)
        {
            // microfarads
            capacitance *= 1000000d;
            int c = (int) Math.Floor(capacitance);
            CapacitorText.text = capText = c + " μF ±10%";
        }
        else
        {
            // nanofarads
            capacitance *= 1000000000d;
            int c = (int) Math.Floor(capacitance);
            CapacitorText.text = capText = c + " nF ±10%";
        }
        Resistor1Bands[0].material = BandColors[resistences[0] / 100];
        Resistor2Bands[0].material = BandColors[resistences[1] / 100];
        Resistor3Bands[0].material = BandColors[resistences[2] / 100];

        Resistor1Bands[1].material = BandColors[(resistences[0] / 10) % 10];
        Resistor2Bands[1].material = BandColors[(resistences[1] / 10) % 10];
        Resistor3Bands[1].material = BandColors[(resistences[2] / 10) % 10];

        Resistor1Bands[2].material = BandColors[resistences[0] % 10];
        Resistor2Bands[2].material = BandColors[resistences[1] % 10];
        Resistor3Bands[2].material = BandColors[resistences[2] % 10];

        Resistor1Bands[3].material = BandColors[exponents[0]];
        Resistor2Bands[3].material = BandColors[exponents[1]];
        Resistor3Bands[3].material = BandColors[exponents[2]];

        Debug.LogFormat("[The Black Button #{0}] The resistors' values are (from top to bottom): {1}Ω {2}Ω {3}Ω", _moduleId, resistences[0] * Math.Pow(10, exponents[0]), resistences[1] * Math.Pow(10, exponents[1]), resistences[2] * Math.Pow(10, exponents[2]));
        Debug.LogFormat("[The Black Button #{0}] The capacitor's value is: {1}", _moduleId, capText.Substring(0, capText.Length - 5));

        double realCapacitence = int.Parse(capText.Substring(0, capText.Length - 8));
        double minResistence = 1 / (1 / (0.9 * resistences[1] * Math.Pow(10, exponents[1])) + 1 / (0.9 * resistences[2] * Math.Pow(10, exponents[2])) + 1d / (0.9 * resistences[0] * Math.Pow(10, exponents[0])));
        double maxResistence = 1 / (1 / (1.1 * resistences[1] * Math.Pow(10, exponents[1])) + 1 / (1.1 * resistences[2] * Math.Pow(10, exponents[2])) + 1d / (1.1 * resistences[0] * Math.Pow(10, exponents[0])));

        _minTime = (minResistence * realCapacitence * 0.9d) / (capText.Substring(capText.Length - 7, 1) == "μ" ? 1000000d : 1000000000d);
        _maxTime = (maxResistence * realCapacitence * 1.1d) / (capText.Substring(capText.Length - 7, 1) == "μ" ? 1000000d : 1000000000d);

        Debug.LogFormat("[The Black Button #{0}] Hold for between {1} and {2} seconds.", _moduleId, _minTime, _maxTime);
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        _lastHeldTime = Time.time;
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved && _lastHeldTime != null)
        {
            if (Time.time - _lastHeldTime.Value >= _minTime && Time.time - _lastHeldTime.Value <= _maxTime)
            {
                Debug.LogFormat("[The Black Button #{0}] Correct. Module solved.", _moduleId);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                Module.HandlePass();
                _moduleSolved = true;
            }
            else
            {
                Debug.LogFormat("[The Black Button #{0}] You held for {1} seconds. That was incorrect. Strike!", _moduleId, Time.time - _lastHeldTime.Value);
                Module.HandleStrike();
            }
        }
        _lastHeldTime = null;
    }

    void Update()
    {
        SecondsText.text = _lastHeldTime == null ? "-" : Mathf.FloorToInt(Time.time - _lastHeldTime.Value).ToString();
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} hold for 12 [holds the button for 12 seconds] | !{0} capacitor [reads the number on the capacitor]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_moduleSolved)
            yield break;

        var m = Regex.Match(command, @"^\s*hold\s+for\s+(\d\d?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        int v;
        if (m.Success && int.TryParse(m.Groups[1].Value, out v))
        {
            yield return null;
            ButtonSelectable.OnInteract();
            while (Time.time - _lastHeldTime < v)
                yield return null;
            ButtonSelectable.OnInteractEnded();
            yield return new WaitForSeconds(.1f);
        }

        var n = Regex.Match(command, @"^\s*capacitor\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (n.Success)
        {
            yield return null;
            yield return "sendtochat The capacitance of the capacitor is "+ CapacitorText.text + ".";
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        ButtonSelectable.OnInteract();
        while (Time.time - _lastHeldTime < _minTime)
            yield return null;
        ButtonSelectable.OnInteractEnded();
        yield return new WaitForSeconds(.1f);
    }
}
