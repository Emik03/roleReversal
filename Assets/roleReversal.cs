using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using KModkit;

public class roleReversal : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable[] btn;
    public KMSelectable submit;
    public TextMesh screenText, submitText;
    public Component background;

    private bool _lightsOn = false, _isSolved = false, _displayWin = false;
    private sbyte _wireSelected = 0, _correctWire = 0, _instructionsIndex = 0, _frames = 0;
    private int _moduleId = 0, _seed = 0;
    private string _currentText = "";

    private List<char> _convertedSeed;
    private List<byte> _redWires = new List<byte>(0), _orangeWires = new List<byte>(0), _yellowWires = new List<byte>(0),
                       _greenWires = new List<byte>(0), _blueWires = new List<byte>(0), _purpleWires = new List<byte>(0);

    private char[] _displayText = new char[0];
    private readonly string[] _completeText = new string[9] { "C", "o", "m", "p", "l", "e", "t", "e", "!" };

    private static int _moduleIdCounter = 1;
    private static short moduleCount = 0;

    private Color32 mainColor = new Color32(158, 133, 245, 255);

    /// <summary>
    /// Code that runs during the loading period.
    /// </summary>
    void Start()
    {
        moduleCount = 0;
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
        UpdateColor();
    }

    /// <summary>
    /// Primarily used for animation, runs 50 times per second.
    /// </summary>
    private void FixedUpdate()
    {
        //frame counter
        _frames++;

        //update every fourth
        _frames %= 4;

        if (_isSolved)
        {
            //if color transition is not complete, do the color transition
            if (mainColor.r != 0)
                UpdateColor();

            //display win message
            if (screenText.text.Length < 9 && _displayWin && _frames == 0)
                screenText.text += _completeText[screenText.text.Length];

            //remove text
            else if (!_displayWin)
            {
                if (screenText.text.Length >= 2)
                    screenText.text = screenText.text.Remove(screenText.text.Length - 2, 2);

                else
                {
                    screenText.text = "";
                    _displayWin = true;
                }
            }
        }

        else
        {
            //speed = 1.25 characters per frame
            if (screenText.text.Length < _displayText.Count() - 1 && _frames == 0)
            {
                screenText.text = screenText.text.Insert(screenText.text.Length, _displayText[screenText.text.Length].ToString());
                screenText.text = screenText.text.Insert(screenText.text.Length, _displayText[screenText.text.Length].ToString());
            }

            else if (screenText.text.Length < _displayText.Count())
                screenText.text = screenText.text = screenText.text.Insert(screenText.text.Length, _displayText[screenText.text.Length].ToString());
        }

    }

    /// <summary>
    /// Button initaliser, runs upon loading.
    /// </summary>
    private void Awake()
    {
        submit.OnInteract += delegate ()
        {
            HandleSubmit();
            return false;
        };

        for (int i = 0; i < 5; i++)
        {
            int j = i;
            btn[i].OnInteract += delegate ()
            {
                HandlePress(j);
                return false;
            };
        }
    }

    /// <summary>
    /// Runs when the lights turn on.
    /// </summary>
    void Activate()
    {
        Init();
        _lightsOn = true;
        submitText.text = (_wireSelected + 1).ToString();
    }

    /// <summary>
    /// Generates the seed and runs other methods.
    /// </summary>
    private void Init()
    {
        moduleCount++;

        Debug.LogFormat("");

        //_seed!
        //generate seed and flip to random instruction page
        _seed = Random.Range(0, 279936);
        _instructionsIndex = (sbyte)(Random.Range(0, 45));

        //meme seed for thumbnail
        //_seed = 279935;

        //run this method every time the screen needs to be updated
        DisplayScreen();

        //gives a ton of debug information about the conversion from seed to colored wires
        DisplayDebug();
    }

    /// <summary>
    /// Handles pressing of all buttons and screens (aside from submit)
    /// </summary>
    /// <param name="num">The index for the 5 buttons so the program can differentiate which button was pushed.</param>
    void HandlePress(int num)
    {
        //plays button sound effect
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[num].transform);
        btn[num].AddInteractionPunch();

        //if lights are off, the buttons should do nothing
        if (!_lightsOn || _isSolved) return;

        //0 and 1 are the left and right buttons for top panel that controls wire cutting
        //2 and 3 are the left and right buttons for the bottom panel that controls instructions
        //4 is the bottom panel itself that skips to the next section
        switch (num)
        {
            case 0:
                //top screen, previous selection
                _wireSelected--;
                _wireSelected += 7;
                _wireSelected %= 7;
                submitText.text = (_wireSelected + 1).ToString();
                break;

            case 1:
                //top screen, next selection
                _wireSelected++;
                _wireSelected += 7;
                _wireSelected %= 7;
                submitText.text = (_wireSelected + 1).ToString();
                break;

            case 2:
                //bottom screen, previous instruction
                _instructionsIndex--;
                _instructionsIndex += 45;
                _instructionsIndex %= 45;
                DisplayScreen();
                break;

            case 3:
                //buttom screen, next instruction
                _instructionsIndex++;
                _instructionsIndex += 45;
                _instructionsIndex %= 45;
                DisplayScreen();
                break;

            case 4:
                //skip to the correct sections
                if (_instructionsIndex < 5)
                    _instructionsIndex = 5;

                else if (_instructionsIndex < 11)
                    _instructionsIndex = 11;

                else if (_instructionsIndex < 18)
                    _instructionsIndex = 18;

                else if (_instructionsIndex < 26)
                    _instructionsIndex = 26;

                else if (_instructionsIndex < 35)
                    _instructionsIndex = 35;

                else
                    _instructionsIndex = 0;

                DisplayScreen();
                break;
        }
    }

    /// <summary>
    /// Registers whether the answer provided by the player is correct or not.
    /// </summary>
    void HandleSubmit()
    {
        //plays button sound effect
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        submit.AddInteractionPunch(3);

        //if lights are off or it's solved, the buttons should do nothing
        if (!_lightsOn || _isSolved) return;

        //play cut wire sound effect
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, submit.transform);

        //calculates the answer
        CalculateAnswer();

        Debug.LogFormat("[Role Reversal #{0}] Time when cut: {1} seconds.", _moduleId, Info.GetTime());
        Debug.LogFormat("[Role Reversal #{0}] Role Reversal module count: {1} on the current bomb.", _moduleId, moduleCount);

        //if the answer is correct
        if (_wireSelected + 1 == _correctWire)
        {
            Debug.LogFormat("[Role Reversal #{0}] Wire {1} was cut, correct wire was cut! Module solved!", _moduleId, _wireSelected + 1);

            Audio.PlaySoundAtTransform("ropesSolved", submit.transform);

            //make module solved
            Module.HandlePass();

            //deactivate module
            _isSolved = true;
        }

        else
        {
            Debug.LogFormat("[Role Reversal #{0}] Wire {1} was cut, incorrect wire was cut! Strike!", _moduleId, _wireSelected + 1);

            Audio.PlaySoundAtTransform("ropesStrike", submit.transform);

            //make module strike
            Module.HandleStrike();
        }

        Debug.LogFormat("");
    }

    /// <summary>
    /// Calculates the correct answer, logs it and puts it in the _correctWire variable.
    /// </summary>
    private void CalculateAnswer()
    {
        //reset wire calculation
        GetWires();
        _correctWire = 0;

        //all wires together
        List<List<byte>> _wires = new List<List<byte>>(6) { _redWires, _orangeWires, _yellowWires, _greenWires, _blueWires, _purpleWires };

        //counts the amount of wires
        switch (_convertedSeed.Count)
        {
            //2 wires!
            case 2:
                //if wire colors are the same
                if (_convertedSeed[0] == _convertedSeed[1])
                {
                    _correctWire = 2;
                    Debug.LogFormat("[Role Reversal #{0}] Exception 1 (If both are the same): True, cut wire 2.", _moduleId);
                }

                //if the wire colors are complementary
                else if (Mathf.Abs((float)(char.GetNumericValue(_convertedSeed[0]) - char.GetNumericValue(_convertedSeed[1]))) == 3)
                {
                    if (char.GetNumericValue(_convertedSeed[0]) < char.GetNumericValue(_convertedSeed[1]))
                        _correctWire = 2;

                    else
                        _correctWire = 1;

                    Debug.LogFormat("[Role Reversal #{0}] Exception 2 (If both are complementary): True, cut wire {1}.", _moduleId, _correctWire);
                }

                //if the wire colors are triadic
                else if (Mathf.Abs((float)(char.GetNumericValue(_convertedSeed[0]) - char.GetNumericValue(_convertedSeed[1]))) == 2 || Mathf.Abs((float)(char.GetNumericValue(_convertedSeed[0]) - char.GetNumericValue(_convertedSeed[1]))) == 4)
                {
                    _correctWire = 1;

                    Debug.LogFormat("[Role Reversal #{0}] Exception 3 (If both are triadic): True, cut wire 1.", _moduleId);
                }

                //otherwise
                else
                {
                    if (char.GetNumericValue(_convertedSeed[0]) % 2 == 0)
                        _correctWire = 2;

                    else
                        _correctWire = 1;

                    Debug.LogFormat("[Role Reversal #{0}] Exception 4 (Otherwise): True, cut wire {1}.", _moduleId, _correctWire);
                }
                break;

            //3 wires!
            case 3:
                //if only two wires share color
                if (_convertedSeed.Count - 1 == _convertedSeed.Distinct().Count())
                    for (int i = 0; i < _wires.Count; i++)
                    {
                        if (_wires[i].Count == 1)
                        {
                            _correctWire = (sbyte)(_wires[i][0] + 1);
                            Debug.LogFormat("[Role Reversal #{0}] Exception 1 (If only two wires share color): True, cut wire {1}.", _moduleId, _correctWire);
                            break;
                        }
                    }

                if (_correctWire == 0)
                {
                    //if there is only 1 of this module
                    if (moduleCount == 1)
                    {
                        _correctWire = 3;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 2 (If only one of this module is present): True, cut wire 3.", _moduleId);
                        break;
                    }

                    for (int i = 0; i < _convertedSeed.Count - 1; i++)
                        //if warm color is to the left of cold color
                        if (char.GetNumericValue(_convertedSeed[i]) <= 2 && char.GetNumericValue(_convertedSeed[i + 1]) >= 3)
                            for (int j = _convertedSeed.Count - 2; j > 0; j--)
                                if (char.GetNumericValue(_convertedSeed[j + 1]) >= 3)
                                {
                                    _correctWire = (sbyte)(char.GetNumericValue(_convertedSeed[j]) + 1);
                                    Debug.LogFormat("[Role Reversal #{0}] Exception 3 (If a warm color is to the left of a cold color): True, cut wire {1}.", _moduleId, _correctWire);
                                    break;
                                }

                    if (_correctWire == 0)
                    {
                        //if serial contains letters found in module name
                        if (Info.GetSerialNumberLetters().Any("ROLEVSAL".Contains))
                        {
                            _correctWire = 1;
                            Debug.LogFormat("[Role Reversal #{0}] Exception 4 (If serial contains module name's letters): True, cut wire 1.", _moduleId);
                            break;
                        }

                        //otherwise
                        else
                        {
                            _correctWire = 1;
                            Debug.LogFormat("[Role Reversal #{0}] Exception 5 (Otherwise): True, cut wire 1.", _moduleId);
                        }
                    }
                }
                break;

            //4 wires!
            case 4:
                //if first wire is red
                if (_convertedSeed[0] == '0')
                {
                    _correctWire = (sbyte)(_redWires[_redWires.Count - 1] + 1);
                    Debug.LogFormat("[Role Reversal #{0}] Exception 1 (If first wire is red): True, cut wire {1}.", _moduleId, _correctWire);
                }

                //if less than 1 minute is left
                else if (Info.GetTime() < 60)
                {
                    _correctWire = 1;
                    Debug.LogFormat("[Role Reversal #{0}] Exception 2 (If less than 1 minute is left): True, cut wire 1.", _moduleId);
                }

                //if all wires are unique
                else if (_convertedSeed.Count == _convertedSeed.Distinct().Count())
                {
                    for (int i = 0; i < _convertedSeed.Count; i++)
                    {
                        if (_convertedSeed[i] == '1' || _convertedSeed[i] == '4' || _convertedSeed[i] == '5')
                        {
                            _correctWire = (sbyte)(char.GetNumericValue(_convertedSeed[i]) + 1);
                            Debug.LogFormat("[Role Reversal #{0}] Exception 3 (If all wires are unique): True, cut wire {1}.", _moduleId, _correctWire);
                            break;
                        }
                    }
                }

                //if first wire is a warm color
                else if (_convertedSeed[0] == '1' || _convertedSeed[0] == '2')
                {
                    for (int i = _convertedSeed.Count - 1; i >= 0; i--)
                    {
                        if (char.GetNumericValue(_convertedSeed[i]) <= 2)
                        {
                            _correctWire = (sbyte)(i + 1);
                            Debug.LogFormat("[Role Reversal #{0}] Exception 4 (If first wire is a warm color): True, cut wire {1}.", _moduleId, _correctWire);
                            break;
                        }
                    }
                }

                //if 10+ modules are on the bomb
                else if (Info.GetModuleIDs().Count >= 10)
                {
                    _correctWire = 3;
                    Debug.LogFormat("[Role Reversal #{0}] Exception 5 (If 10+ modules are on the bomb): True, cut wire 3.", _moduleId);
                }

                //otherwise
                else
                {
                    _correctWire = 2;
                    Debug.LogFormat("[Role Reversal #{0}] Exception 6 (Otherwise): True, cut wire 2.", _moduleId);
                }
                break;

            //5 wires!
            case 5:
                //if both red doesn't exist and orange exists
                if (!_convertedSeed.Contains('0') && _convertedSeed.Contains('1'))
                {
                    _correctWire = (sbyte)(_orangeWires[0] + 1);
                    Debug.LogFormat("[Role Reversal #{0}] Exception 2 (If there are any orange wires): True, cut wire 1.", _moduleId, _correctWire);
                }

                else
                {
                    for (int i = 0; i < _convertedSeed.Count - 1; i++)
                    {
                        //if yellow wire is to the left of green wire
                        if (_convertedSeed[i] == '2' && _convertedSeed[i + 1] == '3')
                        {
                            _correctWire = (sbyte)(_yellowWires[0] + 1); ;
                            Debug.LogFormat("[Role Reversal #{0}] Exception 3 (If yellow wire to the left of green wire): True, cut wire {1}.", _moduleId, _correctWire);
                            break;
                        }

                        //if yellow wire is to the right of green wire
                        if (_convertedSeed[i] == '3' && _convertedSeed[i + 1] == '2')
                        {
                            _correctWire = (sbyte)(_greenWires[0] + 1);
                            Debug.LogFormat("[Role Reversal #{0}] Exception 4 (If yellow wire to the right of green wire): True, cut wire {1}.", _moduleId, _correctWire);
                            break;
                        }
                    }

                    //if correctWire hasn't been set
                    if (_correctWire == 0)
                    {
                        //really only for purple wires
                        GetWires();

                        //if one purple wire exists
                        if (_purpleWires.Count == 1)
                        {
                            _correctWire = (sbyte)(_purpleWires[0] + 1);
                            Debug.LogFormat("[Role Reversal #{0}] Exception 5 (If one purple wire exists): True, cut wire {1}.", _moduleId, _correctWire);
                        }

                        //if any indicators are off
                        else if (Info.GetOffIndicators().Count() > 0)
                        {
                            _correctWire = 3;
                            Debug.LogFormat("[Role Reversal #{0}] Exception 6 (If any off indicators are off): True, cut wire 3.", _moduleId);
                        }

                        //otherwise
                        else
                        {
                            _correctWire = 2;
                            Debug.LogFormat("[Role Reversal #{0}] Exception 7 (Otherwise): True, cut wire 2.", _moduleId);
                        }
                    }
                }
                break;

            //6 wires!
            case 6:
                //if there aren't 2 numbers in serial
                if (Info.GetSerialNumberNumbers().Count() != 2)
                {
                    //if serial has a vowel
                    if (Info.GetSerialNumberLetters().Any("AEIOU".Contains))
                    {
                        _correctWire = 6;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 3 (If serial has a vowel): True, cut wire 6.", _moduleId);
                        break;
                    }

                    //if all primary colors exist
                    else if (_convertedSeed.Contains('0') && _convertedSeed.Contains('2') && _convertedSeed.Contains('4'))
                        for (int i = 0; i < _convertedSeed.Count; i++)
                            if (_convertedSeed[i] == 1 || _convertedSeed[i] == 4 || _convertedSeed[i] == 5)
                            {
                                _correctWire = (sbyte)(i + 1);
                                Debug.LogFormat("[Role Reversal #{0}] Exception 2 (If all primary colors exist): True, cut wire {1}.", _moduleId, _correctWire);
                                break;
                            }
                }

                if (_correctWire == 0)
                {
                    byte pairs = 0;
                    byte triplets = 0;

                    //if 2 pairs or 1 triplet
                    for (int i = 0; i < _wires.Count; i++)
                    {
                        //check for pairs
                        if (_wires[i].Count == 2)
                            pairs++;

                        //check for triplets
                        else if (_wires[i].Count == 3)
                            triplets++;
                    }

                    //if there is a triplet, or 2 pairs were detected previously
                    if (pairs == 2 || triplets == 1)
                    {
                        /*
                         * runs through the order of the wires, based on what numerical value is in each wire,
                         * it runs through the index of _wires, which will check whether there is one member
                         * the specified color. if not, it continues through the list until it finds one.
                        */
                        for (int i = 0; i < _convertedSeed.Count; i++)
                            if (_wires[(int)char.GetNumericValue(_convertedSeed[i])].Count == 1)
                            {
                                _correctWire = (sbyte)(_wires[System.Convert.ToSByte(char.GetNumericValue(_convertedSeed[i]))][0] + 1);
                                Debug.LogFormat("[Role Reversal #{0}] Exception 4 (If only 2 pairs or only 1 triplet exist): True, cut wire {1}.", _moduleId, _correctWire);
                                break;
                            }
                    }

                    //if seed is divisible by 3
                    else if (_seed % 3 == 0)
                    {
                        _correctWire = 4;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 5 (If seed is divisible by 3): True, cut wire 4.", _moduleId);
                    }

                    //if more than 600 seconds remain
                    else if (Info.GetTime() > 600)
                    {
                        _correctWire = 2;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 6 (If more than 10 minutes remain): True, cut wire 2.", _moduleId);
                    }

                    //if seed is even
                    else if (_seed % 2 == 0)
                    {
                        _correctWire = 5;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 7 (If seed is even): True, cut wire 5.", _moduleId);
                    }

                    //otherwise
                    else
                    {
                        _correctWire = 3;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 8 (Otherwise): True, cut wire 3.", _moduleId);
                    }
                }
                break;

            //7 wires!
            case 7:
                //if there aren't more unlit than lit
                if (Info.GetOnIndicators().Count() < Info.GetOffIndicators().Count())
                {
                    //for cold colors
                    GetWires();

                    //if first, fourth, or last share any same color
                    if (_convertedSeed[0] == _convertedSeed[3] || _convertedSeed[0] == _convertedSeed[6] || _convertedSeed[3] == _convertedSeed[6])
                    {
                        _correctWire = 4;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 2 (If first, fourth or last wire share any same colors): True, cut wire 4.", _moduleId, _correctWire);
                        break;
                    }

                    //if there are 2 blue wires
                    else if (_blueWires.Count >= 2)
                    {
                        _correctWire = (sbyte)(_blueWires[0] + 2);
                        Debug.LogFormat("[Role Reversal #{0}] Exception 3 (If there are 2 blue wires): True, cut wire {1}.", _moduleId, _correctWire);
                        break;
                    }

                    //if there aren't 2 purple wires
                    else if (_purpleWires.Count != 2)
                    {
                        _correctWire = 7;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 4 (If there aren't 2 purple wires): True, cut wire 7.", _moduleId);
                        break;
                    }
                }

                //for every wire
                GetWires();

                //if there is CAR or FRK label
                if (Info.GetIndicators().Any("CAR".Contains) || Info.GetIndicators().Any("FRK".Contains))
                {
                    _correctWire = (sbyte)((Info.GetOnIndicators().Count() % 7) + 1);
                    Debug.LogFormat("[Role Reversal #{0}] Exception 5 (If there is CAR or FRK label): True, cut wire {1}.", _moduleId, _correctWire);
                }

                //if serial has a matching number to red wires
                else if (Info.GetSerialNumber().Contains(System.Convert.ToChar(_redWires.Count + 48)))
                {
                    _correctWire = 6;
                    Debug.LogFormat("[Role Reversal #{0}] Exception 6 (If the serial has a matching number to the number of red wires present): True, cut wire 6.", _moduleId);
                }

                //if there are less batteries than orange wires
                else if (Info.GetBatteryCount() < _orangeWires.Count)
                {
                    _correctWire = (sbyte)(_orangeWires[_orangeWires.Count - 1] + 1);
                    Debug.LogFormat("[Role Reversal #{0}] Exception 7 (If there are less batteries than orange wires): True, cut wire {1}.", _moduleId, _correctWire);
                }

                else
                {
                    //if 3 or more wires share the same color
                    for (int i = 0; i < _wires.Count; i++)
                        if (_wires[i].Count >= 3)
                        {
                            _correctWire = 6;
                            Debug.LogFormat("[Role Reversal #{0}] Exception 8 (If 3 or more wires share the same color): True, cut wire 6.", _moduleId);
                        }

                    //otherwise
                    if (_correctWire == 0)
                    {
                        _correctWire = 3;
                        Debug.LogFormat("[Role Reversal #{0}] Exception 9 (Otherwise): True, cut wire 3.", _moduleId);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Counts the amount of wires and keeps track of their position.
    /// </summary>
    private void GetWires()
    {
        //reset all lists in case if it was ran previously
        _redWires = new List<byte>(0);
        _orangeWires = new List<byte>(0);
        _yellowWires = new List<byte>(0);
        _greenWires = new List<byte>(0);
        _blueWires = new List<byte>(0);
        _purpleWires = new List<byte>(0);

        //count number of wires that are any color
        for (int i = 0; i < _convertedSeed.Count; i++)
        {
            switch (_convertedSeed[i])
            {
                case '0':
                    _redWires.Add((byte)i);
                    break;

                case '1':
                    _orangeWires.Add((byte)i);
                    break;

                case '2':
                    _yellowWires.Add((byte)i);
                    break;

                case '3':
                    _greenWires.Add((byte)i);
                    break;

                case '4':
                    _blueWires.Add((byte)i);
                    break;

                case '5':
                    _purpleWires.Add((byte)i);
                    break;
            }
        }

    }


    /// <summary>
    /// Updates what needs to be displayed on screen.
    /// </summary>
    private void DisplayScreen()
    {
        _currentText = "";
        _currentText += "Seed: " + _seed.ToString();
        _currentText += "\n\n" + _instructions[_instructionsIndex];

        _displayText = new char[_currentText.Length];
        _displayText = _currentText.ToCharArray();

        screenText.text = "";
    }

    /// <summary>
    /// Logs information about the module's seed, wires, and their colors.
    /// </summary>
    private void DisplayDebug()
    {
        Debug.LogFormat("[Role Reversal #{0}] Seed: {1}", _moduleId, _seed);

        _convertedSeed = ConvertToB6(_seed);

        string _colorList = "";

        foreach (char wire in _convertedSeed)
        {
            switch (wire)
            {
                case '0':
                    _colorList += "Red ";
                    break;

                case '1':
                    _colorList += "Orange ";
                    break;

                case '2':
                    _colorList += "Yellow ";
                    break;

                case '3':
                    _colorList += "Green ";
                    break;

                case '4':
                    _colorList += "Blue ";
                    break;

                case '5':
                    _colorList += "Purple ";
                    break;
            }
        }

        Debug.LogFormat("[Role Reversal #{0}] Final Wires: {1}", _moduleId, _colorList);

        Debug.LogFormat("");
    }

    /// <summary>
    /// Converts any number from base-10 to base-6.
    /// </summary>
    /// <param name="num">The base-10 number provided to convert the number.</param>
    /// <returns>The variable provided but in base-6.</returns>
    private List<char> ConvertToB6(int num)
    {
        string B6number = "";
        byte wireCount = (byte)(num % 6 + 2);

        Debug.LogFormat("[Role Reversal #{0}] Amount of Wires: {1}", _moduleId, wireCount);

        while (num >= 6)
        {
            B6number += System.Convert.ToString(num % 6);
            num /= 6;
        }

        B6number += System.Convert.ToString(num);
        List<char> result = B6number.Reverse().ToList();

        while (wireCount < result.Count)
            result.RemoveAt(result.Count - 1);

        //ensures that if the base 6 conversion yields less than 7 wires, it should add leading 0's
        while (result.Count < wireCount)
            result.Insert(0, '0');

        return result;
    }

    /// <summary>
    /// Updates the color of all objects within the module.
    /// </summary>
    private void UpdateColor()
    {
        //fade out rgb
        mainColor.r += (byte)((0 - mainColor.r) / 100);
        mainColor.g += (byte)((200 - mainColor.g) / 100);
        mainColor.b += (byte)((255 - mainColor.b) / 100);

        //change all colors
        submitText.color = mainColor;
        screenText.color = mainColor;

        //background changes color
        background.GetComponent<MeshRenderer>().material.color = new Color32((byte)(mainColor.r / 2), (byte)(mainColor.g / 2), (byte)(mainColor.b / 2), 255);

        //fifth button is a screen, so it isn't included here
        for (int i = 0; i < 4; i++)
            btn[i].GetComponent<MeshRenderer>().material.color = mainColor;

        //both top and bottom panel
        submit.GetComponent<MeshRenderer>().material.color = new Color32((byte)(mainColor.r / 10), (byte)(mainColor.g / 10), (byte)(mainColor.b / 10), 255);
        btn[4].GetComponent<MeshRenderer>().material.color = new Color32((byte)(mainColor.r / 10), (byte)(mainColor.g / 10), (byte)(mainColor.b / 10), 255);
    }

    private readonly string[] _instructions = new string[45]
    {
        //0
        "2 Wires",

        "2 Wires (Exception: 1)\n\nIf both wires are\ncolored the same,\ncut the second wire.",
        "2 Wires (Exception: 2)\n\nIf both wires are\ncomplemetary to\neach other, cut the\ncold-colored wire.",
        "2 Wires (Exception: 3)\n\nIf both wires are\ntriadic to each other,\ncut the first wire.",
        "2 Wires (Exception: 4)\n\nOtherwise, cut the\nfirst secondary-\ncolored wire.",

        //5
        "3 Wires",

        "3 Wires (Exception: 1)\n\nIf only two of the\nwires share the\nsame color, cut\nthe unique wire.",
        "3 Wires (Exception: 2)\n\nIf there is only one\nRole Reversal\nmodule, cut the\nthird wire.",
        "3 Wires (Exception: 3)\n\nIf any warm color\ncomes before a cold\none, cut the third wire.",
        "3 Wires (Exception: 4)\n\nIf the serial contains\nany letters found in\nthe module name,\ncut the first wire.",
        "3 Wires (Exception: 5)\n\nOtherwise, cut the\nfirst wire.",

        //11
        "4 Wires",

        "4 Wires (Exception: 1)\n\nIf the first wire is\nred, cut the\nlast red wire.",
        "4 Wires (Exception: 2)\n\nIf less than 1 minute\nis remaining, cut\nthe first wire.",
        "4 Wires (Exception: 3)\n\nIf there are 4 unique\ncolored wires, cut\nthe first wire with\nvalues 1, 4, or 5.",
        "4 Wires (Exception: 4)\n\nIf the first wire is a\nwarm color, cut the\nlast warm-colored\nwire.",
        "4 Wires (Exception: 5)\n\nIf the bomb has 10 or\nmore modules, cut\nthe third wire.",
        "4 Wires (Exception: 6)\n\nOtherwise, cut the\nsecond wire.",

        //18
        "5 Wires",

        "5 Wires (Exception: 1)\n\nIf there are any red\nwires, skip the\nnext page.",
        "5 Wires (Exception: 2)\n\nIf there are any orange\nwires, cut the first\norange wire.",
        "5 Wires (Exception: 3)\n\nIf any yellow wires lie\nadjacently left to a\ngreen wire, cut the\nfirst yellow wire.",
        "5 Wires (Exception: 4)\n\nIf any yellow wires lie\nadjacently right to a\ngreen wire, cut the\nfirst green wire.",
        "5 Wires (Exception: 5)\n\nIf there is only one\npurple wire, cut that\npurple wire.",
        "5 Wires (Exception: 6)\n\nIf any indicators\nare off, cut the\nthird wire.",
        "5 Wires (Exception: 7)\n\nOtherwise, cut the\nsecond wire.",

        //26
        "6 Wires",

        "6 Wires (Exception: 1)\n\nIf the serial has\nexactly 2 digits,\nskip to exception 4.",
        "6 Wires (Exception: 2)\n\nIf the serial has\nany vowel, cut\nthe sixth wire.",
        "6 Wires (Exception: 3)\n\nIf all primary colors\nexist, cut the first\nwire that has its last\nletter an E.",
        "6 Wires (Exception: 4)\n\nIf the seed is\ndivisible by 3, cut\nthe fourth wire.",
        "6 Wires (Exception: 5)\n\nIf exactly 2 pairs\nor exactly 1 triplet\nmatch colors, cut the\nfirst unique wire.",
        "6 Wires (Exception: 6)\n\nIf more than 10\nminutes are\nremaining, cut\nthe second wire.",
        "6 Wires (Exception: 7)\n\nIf the seed is even,\ncut the fifth wire.",
        "6 Wires (Exception: 8)\n\nOtherwise, cut the\nthird wire.",

        //35
        "7 Wires",

        "7 Wires (Exception: 1)\n\nIf there are as many\nor more lit indicators\nas unlit indicators,\nskip to exception 5.",
        "7 Wires (Exception: 2)\n\nIf the first, fourth,\nor last wire share\nany color, cut the\nfourth wire.",
        "7 Wires (Exception: 3)\n\nIf there are 2 or more\nblue wires, cut the\nwire after the first\nblue wire.",
        "7 Wires (Exception: 4)\n\nIf there aren't exactly\n2 purple wires, cut\nthe seventh wire.",
        "7 Wires (Exception: 5)\n\nIf indicator FRK or\nCAR exist, cut\n(N modulo 7) + 1 for\nN lit indicators.",
        "7 Wires (Exception: 6)\n\nIf the serial has any\nmatching numbers to\namount of red wires,\ncut the sixth wire.",
        "7 Wires (Exception: 7)\n\nIf there are less\nbatteries than orange\nwires, cut the\nlast orange wire.",
        "7 Wires (Exception: 8)\n\nIf there are 3 or\nmore of the same\ncolors, cut the\n sixth wire.",
        "7 Wires (Exception: 9)\n\nOtherwise, cut the\nthird wire.",
    };
}