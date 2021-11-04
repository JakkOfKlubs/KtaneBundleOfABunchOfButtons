using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class AubergineButtonScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ButtonSelectable;
    public GameObject ButtonCap;
    public TextMesh Text;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;
    private string _wordlist = "あたまからだせなかこゆびほのおこおりにほんたまごさらださかなくじららくだきりんぱんださくらひかりぼたんいのちからす";
    private string _moduleWord;
    private int _solutionIndex;
    private int _textIndex;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        ButtonSelectable.OnInteract += ButtonPress;
        ButtonSelectable.OnInteractEnded += ButtonRelease;
        int wordIndex = Rnd.Range(0, 20);
        _moduleWord = _wordlist.Substring(3 * wordIndex, 3);
        List<char> potentalReplaces = _wordlist.ToList();
        List<char> mutableWord = _moduleWord.ToList();
        potentalReplaces.RemoveAll(x => _moduleWord.Contains(x));
        _solutionIndex = Rnd.Range(0, 2);
        mutableWord[_solutionIndex] = potentalReplaces[Rnd.Range(0, potentalReplaces.Count)];
        _moduleWord = mutableWord.Join("");
        Debug.LogFormat("[The Aubergine Button #{0}] The hiragana on the module are {1}, and the actual word is {2}.", _moduleId, _moduleWord, _wordlist.Substring(3 * wordIndex, 3));
        StartCoroutine(CycleHiragana());
    }

    private bool ButtonPress()
    {
        StartCoroutine(AnimateButton(0f, -0.05f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        if (!_moduleSolved)
        {
            Debug.LogFormat("[The Aubergine Button #{0}] You pressed the button when the hiragana was {1}.", _moduleId, _moduleWord[_textIndex]);
            if (_textIndex == _solutionIndex)
            {
                Debug.LogFormat("[The Aubergine Button #{0}] Correct. Module solved.", _moduleId);
                _moduleSolved = true;
                Module.HandlePass();
            }
            else
            {
               Debug.LogFormat("[The Aubergine Button #{0}] Incorrect. Strike!", _moduleId);
                Module.HandleStrike();
            }
        }
        return false;
    }

    private void ButtonRelease()
    {
        StartCoroutine(AnimateButton(-0.05f, 0f));
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
    }

    private IEnumerator CycleHiragana()
    {
        _textIndex = 0;
        while (!_moduleSolved)
        {
            Text.text = _moduleWord[_textIndex].ToString();
            yield return new WaitForSeconds(0.5f);
            _textIndex++;
            if (_textIndex == 3)
                _textIndex = 0;
        }       
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
    private readonly string TwitchHelpMessage = "!{0} ま [submits that hirgana]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim();
        if (_moduleSolved)
            yield break;
        if (command.Length != 1)
            yield break;
        if (!_moduleWord.Contains(command))
        {
            yield return "sendtochaterror Hiragana not on module";
            yield break;
        }
        yield return null;
        while (_textIndex != Array.IndexOf(_moduleWord.ToCharArray(), command[0]))
        {
            yield return null;
        }
        ButtonSelectable.OnInteract();
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (_textIndex != _solutionIndex)
        {
            yield return null;
        }
        ButtonSelectable.OnInteract();
    }
}