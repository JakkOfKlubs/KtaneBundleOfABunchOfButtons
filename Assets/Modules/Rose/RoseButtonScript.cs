using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoseButtonScript : MonoBehaviour {
    public KMBombModule Module;
    public KMAudio Audio;
    public TextMesh Timer;
    public KMSelectable RoseButton;
    public GameObject RoseButtonCap;
    public MeshRenderer Base;
    public Material[] BaseSymbols;

    static int _moduleIdCounter = 1;
    private int _moduleId;
    private Coroutine _stopwatch;
    private int _baseIndex = 0;
    private int _outCounter = 0;
    private bool _moduleSolved;

    void Start() {
        _moduleId = _moduleIdCounter++;
        RoseButton.OnInteract += RoseButtonPress;
        RoseButton.OnInteractEnded += RoseButtonRelease;
    }

    private bool RoseButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
            _stopwatch = StartCoroutine(RunStopwatch());
        return false;
    }

    private void RoseButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        if (!_moduleSolved)
        {
            StopCoroutine(_stopwatch);
            JudgePlayer();
        }
    }

    private void JudgePlayer()
    {
        Debug.LogFormat("[The Rose Button {0}] You released the button at {1}.", _moduleId, Timer.text);
        var nonOuts = new Dictionary<string, int>()
        {
            {"0.97", 1},
            {"0.98", 2},
            {"0.99", 3},
            {"1.00", 4}
        };
        int score;
        if (nonOuts.TryGetValue(Timer.text, out score))
        {
            Debug.LogFormat("[The Rose Button {0}] {1}", _moduleId, score == 4 ? "A home run!" : string.Format("A hit! Advancing the runner {0} base{1}.", score, score == 1 ? "" : "s"));
            _baseIndex += score;
            if (_baseIndex > 3)
            {
                Base.enabled = false;
                Debug.LogFormat("[The Rose Button {0}] Module solved.", _moduleId);
                Audio.PlaySoundAtTransform("RoseButtonSolve", transform);
                _moduleSolved = true;
                Module.HandlePass();
                return;
            }
            Base.material = BaseSymbols[_baseIndex];
            return;
        }
        Debug.LogFormat("[The Rose Button {0}] An out!", _moduleId);
        StartCoroutine(OutFlash());
        _outCounter++;
        if (_outCounter == 3)
        {
            Debug.LogFormat("[The Rose Button {0}] 3 outs. Strike!", _moduleId);
            _baseIndex = 0;
            Base.material = BaseSymbols[_baseIndex];
            Module.HandleStrike();
            _outCounter = 0;
        }
    }

    private IEnumerator RunStopwatch()
    {
        double time = 0;
        Timer.text = "0.00";
        while (true)
        {
            time += 0.01;
            Timer.text = (time.ToString() + (time.ToString().Length == 1 ? ".00" : "00")).Substring(0, 4);
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator OutFlash()
    {
        yield return FadeText(true);
        Timer.text = "OUT";
        yield return FadeText(false);
        yield break;
    }

    private IEnumerator FadeText(bool inOrOut)
    {
        var startTime = Time.time;
        var fadeDuration = 0.3f;
            while ((Time.time - startTime) < fadeDuration)
            {
                var v = inOrOut ? 1 - ((Time.time - startTime) / fadeDuration) : (Time.time - startTime) / fadeDuration;
                Timer.color = new Color(v, v, v, 1);
                yield return null; 
            }
        yield break;
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            RoseButtonCap.transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        RoseButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
    }
}