using System.Diagnostics;
using SnelToetsenSjezer.Domain.Enums;
using SnelToetsenSjezer.Domain.Interfaces;
using SnelToetsenSjezer.Domain.Models;
using SnelToetsenSjezer.Domain.Types;
using Timer = System.Windows.Forms.Timer;

namespace SnelToetsenSjezer.Business;

public class HotKeyGameService : IHotKeyGameService
{
    private List<HotKey> _gameHotKeys = new() { };
    private static PressedKeysDict _currentlyPressedKeys = new();

    private Action<string, GameStateCallbackData> gameStateUpdatedCallback = null;
    private Action<int, bool> gameTimerCallback = null;

    private static Timer? _gameTimer = null;
    private static int _gameSeconds = 0;

    private static bool _isPaused = false;
    private static readonly int _pauseDurationDefault = 2;
    private static int _pauseDuration = 0;

    private List<List<string>> _userInputSteps = new();

    private int _currHotKey = 0;
    private bool _dealingWithFails = false;

    public void SetHotKeys(List<HotKey> hotKeys)
    {
        hotKeys.ForEach(hotKey =>
        {
            hotKey.ResetForNewGame();
        });
        _gameHotKeys = hotKeys;
    }

    public void SetGameStateUpdatedCallback(Action<string, GameStateCallbackData> callback)
    {
        gameStateUpdatedCallback = callback;
    }

    public void SetGameTimerCallback(Action<int, bool> callback)
    {
        gameTimerCallback = callback;
    }

    public void StartGame()
    {
        Debug.WriteLine("Starting game!");
        if (_gameTimer != null) _gameTimer.Dispose();
        _gameSeconds = 0;
        _gameTimer = new Timer();
        _gameTimer.Interval = 1000;
        _gameTimer.Tick += new EventHandler(GameTimer_Tick);
        _gameTimer.Start();

        GameStateCallbackData stateData = new()
        {
            { "index", "1" },
            { "count", _gameHotKeys.Count().ToString() },
            { "category", _gameHotKeys[_currHotKey].Category },
            { "description", _gameHotKeys[_currHotKey].Description }
        };

        gameStateUpdatedCallback("playing", stateData);
    }

    public void StopGame(bool forceStop = false)
    {
        Debug.WriteLine("Stopping game!");
        _gameTimer!.Stop();

        _currHotKey = 0;
        _dealingWithFails = false;
        _userInputSteps = new List<List<string>>();

        if (!forceStop) gameStateUpdatedCallback("finished", new GameStateCallbackData());
    }

    public void PauseGame()
    {
        Debug.WriteLine("Pausing game!");
        _isPaused = true;
        _pauseDuration = _pauseDurationDefault;
    }

    public void ResumeGame()
    {
        Debug.WriteLine("Resuming game!");
        _isPaused = false;
        NextHotKey();
    }

    public void GameTimer_Tick(object sender, EventArgs e)
    {
        if (!_isPaused)
        {
            _gameSeconds++;
            _gameHotKeys[_currHotKey].Seconds++;
        }
        else
        {
            if (_pauseDuration > 0)
            {
                _pauseDuration--;
            }
            else
            {
                ResumeGame();
            }
        }
        gameTimerCallback(_gameSeconds, _isPaused);
    }

    // As far as I know, VS responds to KeyDown events, keeping a key pressed fires multiple
    // of them. So there should be a response to each keydown event!
    // Also note that (looking at the ConsoleKey class) Windows considers two kinds of keys to
    // exist: modifier keys (Ctrl, Alt, Shift) and main keys (any other key)
    // This means that one should likely respond differently to a modifier key being pressed
    // and a main key being pressed/held.
    // PSEUDOCODE:

    // On KeyDown:
    //   if (keyName in ModifierKeys) add keyName to set of activemodifierKeys
    //   else // keyname refers to main key
    //       add a "keycombo" of the key with the currently active modifier keys to the
    //           keycombos list
    //       check whether the keycombos-list can be the start of the desired hotkey sequence
    public void KeyDown(string keyName)
    {
        if (!_currentlyPressedKeys.ContainsKey(keyName) || !_currentlyPressedKeys[keyName])
        {
            Debug.WriteLine("KeyDown: " + keyName);
            _currentlyPressedKeys[keyName] = true;

            gameStateUpdatedCallback("userinputsteps", new GameStateCallbackData() {
                { "userinputsteps", GetUserInputSteps(true) }
            });
        }
    }

    // On KeyUp
    // if (keyName in ModifierKeys) remove keyName from set of activemodifierKeys
    // (otherwise do nothing!)
    public void KeyUp(string keyName)
    {
        if (_currentlyPressedKeys.ContainsKey(keyName))
        {
            Debug.WriteLine("KeyUp: " + keyName);
            Debug.WriteLine("- _currentlyPressedKeys.Keys: " + String.Join("+", _currentlyPressedKeys.Keys));

            bool dontAdd = false;
            int containsKeyCount = 0;
            if (_userInputSteps.Count() > 0)
            {
                List<string> previousStep = _userInputSteps[_userInputSteps.Count() - 1];
                _currentlyPressedKeys.Keys.ToList().ForEach(key =>
                {
                    if (previousStep.Contains(key) && previousStep.Count() > 1) containsKeyCount++;
                });
                if (containsKeyCount == _currentlyPressedKeys.Keys.Count())
                {
                    dontAdd = true;
                }
            }
            if (!dontAdd) _userInputSteps.Add(_currentlyPressedKeys.Keys.ToList());
            _currentlyPressedKeys.Remove(keyName);

            CheckForProgressOrFail();
        }
    }

    // the below is probably not needed, as KeyDown already gathers the users KeyCombos/SolutionSteps
    public string GetUserInputSteps(bool includeCurrentlyPressed = false)
    {
        string usrInputSteps = "";
        _userInputSteps.ToList().ForEach(step =>
        {
            string stepStr = "";
            step.ToList().ForEach(sub_step =>
            {
                stepStr += stepStr.Length > 0 ? "+" + sub_step : sub_step;
            });
            usrInputSteps += usrInputSteps.Length > 0 ? "," + stepStr : stepStr;
        });
        if (includeCurrentlyPressed)
        {
            string currentlyPressed = String.Join("+", _currentlyPressedKeys.Keys);
            usrInputSteps += usrInputSteps.Length > 0 ? "," + currentlyPressed : currentlyPressed;
        }

        return usrInputSteps;
    }

    // the below is probably not needed, as KeyDown already gathers the users KeyCombos/SolutionSteps
    public string GetUserInputPartsAsString(int startIndex, int count)
    {
        string usrInputSteps = "";
        int stepIndex = 0;
        int checkedStepCount = 0;
        _userInputSteps.ToList().ForEach(step =>
        {
            if (stepIndex >= startIndex && checkedStepCount < count)
            {
                string stepStr = "";
                step.ToList().ForEach(sub_step =>
                {
                    stepStr += sub_step;
                });
                usrInputSteps += stepStr;
                checkedStepCount++;
            }
            stepIndex++;
        });
        return usrInputSteps.ToLower();
    }

    // the below is probably not needed, as KeyDown already gathers the users KeyCombos/SolutionSteps
    public void FlattenPartOfUserInputToString(int startIndex, int count)
    {
        List<List<string>> newUserInputSteps = new();

        string usrInputStringPart = "";
        int stepIndex = 0;
        int strLength = 0;

        _userInputSteps.ToList().ForEach(step =>
        {
            if (stepIndex >= startIndex && usrInputStringPart.Length < count)
            {
                string stepStr = "";
                step.ToList().ForEach(sub_step =>
                {
                    if (strLength < count)
                    {
                        stepStr += sub_step;
                        strLength++;
                    }
                });
                usrInputStringPart += stepStr;
                if (usrInputStringPart.Length == count)
                {
                    newUserInputSteps.Add(new List<string>() { usrInputStringPart.ToLower() });
                }
            }
            else
            {
                newUserInputSteps.Add(step);
            }
            stepIndex++;
        });

        _userInputSteps = newUserInputSteps;
    }

    // design (pseudocode)

    // For each possibleSolution in allSolutions
    //     bool answerCanBeCorrect = true;
    //     for each keyCombo in userKeyCombos [keycombo 0 until UserKeyCombos.Count]
    //         if the ith userKeyCombo does NOT match the ith solutionKeyCombo
    //             answerCanBeCorrect = false;
    //             break;
    //     if answerCanBeCorrect and UserKeyCombos.Count == possibleSolutionKeyCombos.Count
    //             return true/accept correct answer
    //     else if answerCanBeCorrect // but not complete yet
    //          return true/wait for further input
    //
    // return incorrect (no possible solution has been a match)
    public void CheckForProgressOrFail()
    {
        Debug.WriteLine("- _userInputSteps: " + GetUserInputSteps());

        HotKey myHotKey = _gameHotKeys[_currHotKey];
        HotKeySolutions hotKeySolutions = myHotKey.Solutions;

        bool hasAnyMatches = false;
        bool failedString = false;
        int solutionsShorterThenInput = 0;

        hotKeySolutions.ForEach(hkSolution =>
        {
            int hkSolutionStepIndex = 0;
            double matchingSteps = 0;
            bool partialStringMatch = false;

            hkSolution.ForEach(hkSolutionStep =>
            {
                if (hkSolutionStepIndex <= _userInputSteps.Count() - 1)
                {
                    List<string> hkSolutionStepStrings = new();

                    hkSolutionStep.ForEach(stepPart =>
                    {
                        string stepPartValue = stepPart.ToString();
                        if (stepPart.Type == SolutionStepPartType.String)
                        {
                            Debug.WriteLine("string step! expected string: " + stepPartValue);
                            string userInputPartAsString = GetUserInputPartsAsString(hkSolutionStepIndex, stepPartValue.Length);

                            if (stepPartValue == userInputPartAsString)
                            {
                                Debug.WriteLine("string step - input steps/parts match the expected string! flatten!");

                                FlattenPartOfUserInputToString(hkSolutionStepIndex, stepPartValue.Length);
                                gameStateUpdatedCallback("userinputsteps", new GameStateCallbackData() {
                                    { "userinputsteps", GetUserInputSteps() }
                                });
                                hkSolutionStepStrings.Add(stepPartValue);
                            }
                            else if (stepPartValue == _userInputSteps[hkSolutionStepIndex][0])
                            {
                                Debug.WriteLine("string step - step matches the expected string!");
                                hkSolutionStepStrings.Add(stepPartValue);
                            }
                            else if (stepPartValue.StartsWith(userInputPartAsString))
                            {
                                Debug.WriteLine("string step - " + stepPartValue + " starts with " + userInputPartAsString);
                                //hkSolutionStepStrings.Add(userInputPartAsString);

                                if (matchingSteps == 0)
                                {
                                    hasAnyMatches = true;
                                    matchingSteps++;
                                }
                                partialStringMatch = true;

                                Debug.WriteLine(" ! hkSolutionStepIndex: " + hkSolutionStepIndex);
                                Debug.WriteLine(" ! flatten length: " + userInputPartAsString.Length);
                                FlattenPartOfUserInputToString(hkSolutionStepIndex, userInputPartAsString.Length);

                                gameStateUpdatedCallback("userinputsteps", new GameStateCallbackData() {
                                    { "userinputsteps", GetUserInputSteps() }
                                });
                            }
                            else
                            {
                                Debug.WriteLine("string step - cant match input to expected string, failed!");
                                failedString = true;
                            }
                        }
                        else
                        {
                            hkSolutionStepStrings.Add(stepPartValue);
                        }
                    });
                    Debug.WriteLine(" > hkSolutionStepStrings: " + string.Join(",", hkSolutionStepStrings));
                    Debug.WriteLine(" > _userInputSteps[hkSolutionStepIndex]: " + string.Join(",", _userInputSteps[hkSolutionStepIndex]));

                    if (hkSolutionStepStrings.SequenceEqual(_userInputSteps[hkSolutionStepIndex]))
                    {
                        Debug.WriteLine(" > Sequence match!");
                        hasAnyMatches = true;
                        matchingSteps++;
                    }
                }
                hkSolutionStepIndex++;
            });
            if (matchingSteps == hkSolution.Count())
            {
                HotKeyIsCorrect();
            }

            // if these dont match then fail? but what about other solutions??
            Debug.WriteLine("matchingSteps: " + matchingSteps);
            Debug.WriteLine("_userInputSteps.Count(): " + _userInputSteps.Count());
            if (_userInputSteps.Count() > matchingSteps && !partialStringMatch)
            {
                solutionsShorterThenInput++;
            }
        });

        if (!hasAnyMatches)
        {
            Debug.WriteLine("No matches at all, fail!");
            HotKeyIsFailed();
        }
        else if (failedString)
        {
            Debug.WriteLine("Failed a string input, fail!");
            HotKeyIsFailed();
        }
        else if (solutionsShorterThenInput == hotKeySolutions.Count())
        {
            Debug.WriteLine("All solutions are shorter then the recieved input, fail!");
            HotKeyIsFailed();
        }
    }

    public void HotKeyIsCorrect()
    {
        gameStateUpdatedCallback("correct", new GameStateCallbackData() {
            { "userinputsteps", GetUserInputSteps() }
        });
        _gameHotKeys[_currHotKey].Failed = false;
        PauseGame();
    }

    public void HotKeyIsFailed()
    {
        _gameHotKeys[_currHotKey].Failed = true;

        string hotKeySolutionStr = "";
        HotKeySolutions hotKeySolutions = _gameHotKeys[_currHotKey].Solutions;
        hotKeySolutions[0].ToList().ForEach(solutionStep =>
        {
            if (hotKeySolutionStr != "") hotKeySolutionStr += ",";
            hotKeySolutionStr += String.Join("+", solutionStep);
        });

        GameStateCallbackData stateData = new()
        {
            { "solution", hotKeySolutionStr },
            { "userinputsteps", GetUserInputSteps() }
        };
        gameStateUpdatedCallback("failed", stateData);
        PauseGame();
    }

    public void NextHotKey()
    {
        _userInputSteps = new List<List<string>>();
        _currentlyPressedKeys = new PressedKeysDict();
        bool finished = false;

        if (!_dealingWithFails && _currHotKey < _gameHotKeys.Count() - 1)
        {
            _currHotKey++;
        }
        else
        {
            int failsCount = _gameHotKeys.Where(hk => hk.Failed == true).ToList().Count();
            if (failsCount > 0)
            {
                if (!_dealingWithFails) _dealingWithFails = true;

                for (int i = 0; i < _gameHotKeys.Count(); i++)
                {
                    if (_gameHotKeys[i].Failed && (i != _currHotKey))
                    {
                        _currHotKey = i;
                        _gameHotKeys[i].Attempt += 1;
                        break;
                    }
                }
            }
            else
            {
                finished = true;
                StopGame();
            }
        }
        if (!finished)
        {
            GameStateCallbackData stateData = new()
            {
                { "index", (_currHotKey+1).ToString() },
                { "count", _gameHotKeys.Count().ToString() },
                { "attempt", _gameHotKeys[_currHotKey].Attempt.ToString() },
                { "category", _gameHotKeys[_currHotKey].Category },
                { "description", _gameHotKeys[_currHotKey].Description },
                { "userinputsteps", "" }
            };
            gameStateUpdatedCallback("playing", stateData);
        }
    }

    public List<HotKey> GetGameHotKeys()
    {
        return _gameHotKeys;
    }

    public int GetGameDuration()
    {
        return _gameSeconds;
    }
}