using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class Cube : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;

    private GameObject cube;

    private KMSelectable upButton, leftButton, rightButton, downButton, clockButton, counterButton, startButton, resetButton;

    private const float maxTime = 10f;
    private float currentTime;
    private TextMesh timerText;

    private TextMesh upColorBlind, leftColorBlind, frontColorBlind, rightColorBlind, backColorBlind, bottomColorBlind;

    private MeshRenderer upSticker, leftSticker, frontSticker, rightSticker, downSticker, backSticker;

    [SerializeField]
    private Material redMaterial, orangeMaterial, yellowMaterial, greenMaterial, blueMaterial, whiteMaterial;

    private Color[,] grid;

    private Dictionary<Color, Material> colorDictionary;

    private Color r = Color.red;
    private Color o = new Color(1, 1, .5f); //doesn't have to be exactly orange
    private Color y = Color.yellow;
    private Color g = Color.green;
    private Color b = Color.blue;
    private Color w = Color.white;

    private bool rotating = false;

    private int row;
    private int col;

    private Color topFace, leftFace, frontFace, rightFace, bottomFace, backFace;

    private int aaBatteryCount, batteryCount, holderCount, dBatteryCount, litIndicatorNum, unlitIndicatorNum, digitsInSerialNum, constantsInSerialNum, lettersInSerialNum, portPlates;

    private string serialNumber;

    private List<string> answer;
    private List<string> inputList;
    private List<Rotation> beforeStartInputList;


    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    private bool timerStarted = false;

    bool disableButtons;

    private Quaternion currentRotation = Quaternion.identity;

    private Dictionary<Rotation, Rotation> oppositeMoves;

    const float inputTime = .5f; //the amount of time it takes for the cube to rotate when button is pressed 

    enum Rotation
    {
        Up,
        Left,
        Right,
        Down,
        Clock,
        Counter
    }

    bool debug = true;
    void Awake()
    {
        ModuleId = ModuleIdCounter++;

        GetComponents();

        grid = new Color[,]
        {
            { g, y, r, w, y, w, y, g, y, r},
            { r, g, w, b, g, r, w, r, g, b},
            { b, o, g, y, r, w, b, g, y, o},
            { y, b, w, r, b, y, w, o, r, b},
            { r, w, b, y, o, r, o, y, b, g},
            { o, r, g, w, r, b, w, o, r, b},
            { b, g, r, b, y, w, y, b, g, w},
            { r, o, y, g, w, b, r, y, w, b},
            { o, w, o, w, g, r, g, b, r, w},
            { y, g, y, b, o, g, b, o, g, o},
        };


        colorDictionary = new Dictionary<Color, Material>()
        {
            [r] = redMaterial,
            [o] = orangeMaterial,
            [y] = yellowMaterial,
            [g] = greenMaterial,
            [b] = blueMaterial,
            [w] = whiteMaterial,
        };

        upButton.OnInteract += delegate () { upButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(Rotate(Rotation.Up, inputTime)); return false; };
        downButton.OnInteract += delegate () { downButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(Rotate(Rotation.Down, inputTime)); return false; };

        leftButton.OnInteract += delegate () { leftButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(Rotate(Rotation.Left, inputTime)); return false; };
        rightButton.OnInteract += delegate () { rightButton.AddInteractionPunch(1f);  if (!disableButtons) StartCoroutine(Rotate(Rotation.Right, inputTime)); return false; };

        clockButton.OnInteract += delegate () { clockButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(Rotate(Rotation.Clock, inputTime)); return false; };
        counterButton.OnInteract += delegate () { counterButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(Rotate(Rotation.Counter, inputTime)); return false; };
        resetButton.OnInteract += delegate () { resetButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(ResetButton()); return false; };

        startButton.OnInteract += delegate () { startButton.AddInteractionPunch(1f); if (!disableButtons) StartCoroutine(StartButton()); return false; };


        oppositeMoves = new Dictionary<Rotation, Rotation>()
        {
            [Rotation.Up] = Rotation.Down,
            [Rotation.Down] = Rotation.Up,
            [Rotation.Left] = Rotation.Right,
            [Rotation.Right] = Rotation.Left,
            [Rotation.Counter] = Rotation.Clock,
            [Rotation.Clock] = Rotation.Counter,
        };
    }

    void Start()
    {
        SetCube();
        GetEdgework();
        GetAnswer();
        ResetModule();

        disableButtons = false;
        beforeStartInputList = new List<Rotation>();
    }

    void ResetModule()
    {
        timerStarted = false;
        currentTime = maxTime;
        inputList = new List<string>();
        timerText.text = currentTime.ToString("00.0");
    }

    void Strike()
    {
        StartCoroutine(ResetButton());
        GetComponent<KMBombModule>().HandleStrike();
        ResetModule();
    }

    void Solve()
    {
        GetComponent<KMBombModule>().HandlePass();
        timerText.text = "GG";
        timerStarted = false;
        ModuleSolved = true;
    }


    void Update()
    {
        if (timerStarted)
        {
            currentTime -= Time.deltaTime;

            timerText.text = currentTime.ToString("00.0");

            if (currentTime <= 0)
            {
                timerStarted = false;
                Strike();
            }
        }
    }

    IEnumerator Rotate(Rotation rotation, float maxTime)
    {
         //maxTime -  how much time the rotation should take 
        if (rotating || ModuleSolved)
            yield break;

        rotating = true;

        Vector3 axis;
        switch (rotation)
        {
            case Rotation.Up:
                axis = Vector3.right;
                break;
            case Rotation.Down:
                axis = -Vector3.right;
                break;
            case Rotation.Right:
                axis = -Vector3.forward;
                break;
            case Rotation.Left:
                axis = Vector3.forward;
                break;
            case Rotation.Clock:
                axis = Vector3.up;
                break;
            default: //counter
                axis = -Vector3.up;
                break;
        }

        axis *= 90;

        Quaternion fromAngle = cube.transform.localRotation;
        currentRotation = Quaternion.Euler(axis) * currentRotation;

        for (var t = 0f; t < 1; t += Time.deltaTime / maxTime)
        {
            cube.transform.localRotation = Quaternion.Lerp(fromAngle, currentRotation, t);
            yield return null;
        }

        cube.transform.localRotation = currentRotation;

        if (timerStarted)
        {
            HandleInput(rotation);
        }

        else
        {
            beforeStartInputList.Add(rotation);
        }

        rotating = false;
    }

    IEnumerator ResetButton()
    {
        disableButtons = true;

        for (int i = beforeStartInputList.Count - 1; i > -1 ; i--)
        {
            Rotation input = beforeStartInputList[i];

            yield return Rotate(oppositeMoves[input], .1f);
        }

        beforeStartInputList.Clear();
        disableButtons = false;
    }

    IEnumerator StartButton()
    {
        yield return ResetButton();
        timerStarted = true;

    }

    void HandleInput(Rotation rotation)
    {
        string input;
        string log = "";

        switch (rotation)
        {
            case Rotation.Clock:
                input = "C";
                break;
            case Rotation.Counter:
                input = "CC";
                break;
            case Rotation.Up:
                input = "U";
                break;
            case Rotation.Left:
                input = "L";
                break;
            case Rotation.Right:
                input = "R";
                break;
            default: //down
                input = "D";
                break;
        }

        inputList.Add(input);

        string expected = answer[inputList.Count - 1];

        if (input != expected)
        {
            log += $"Strike! You entered {string.Join(", ", inputList.ToArray())}";
            Strike();
        }

        else if (inputList.Count == answer.Count)
        {
            log += " YOU BEAT THE WORLD RECORD!!!!";
            Solve();
        }

        Logging(log);
    }

    void GetComponents()
    {
        cube = transform.Find("Cubelet").gameObject;
        upButton = transform.Find("Up Button").GetComponent<KMSelectable>();
        downButton = transform.Find("Down Button").GetComponent<KMSelectable>();
        leftButton = transform.Find("Left Button").GetComponent<KMSelectable>();
        rightButton = transform.Find("Right Button").GetComponent<KMSelectable>();
        clockButton = transform.Find("Clock Button").GetComponent<KMSelectable>();
        counterButton = transform.Find("Counter Button").GetComponent<KMSelectable>();
        startButton = transform.Find("Start Button").GetComponent<KMSelectable>();
        resetButton = transform.Find("Reset Button").GetComponent<KMSelectable>();

        timerText = transform.Find("Timer").transform.Find("Text").GetComponent<TextMesh>();

        upSticker = cube.transform.Find("Up Sticker").GetComponent<MeshRenderer>();
        leftSticker = cube.transform.Find("Left Sticker").GetComponent<MeshRenderer>();
        frontSticker = cube.transform.Find("Front Sticker").GetComponent<MeshRenderer>();
        rightSticker = cube.transform.Find("Right Sticker").GetComponent<MeshRenderer>();
        backSticker = cube.transform.Find("Back Sticker").GetComponent<MeshRenderer>();
        downSticker = cube.transform.Find("Down Sticker").GetComponent<MeshRenderer>();

        upColorBlind = upSticker.transform.Find("Text").GetComponent<TextMesh>();
        leftColorBlind = leftSticker.transform.Find("Text").GetComponent<TextMesh>();
        frontColorBlind = frontSticker.transform.Find("Text").GetComponent<TextMesh>();
        rightColorBlind = rightSticker.transform.Find("Text").GetComponent<TextMesh>();
        backColorBlind = backSticker.transform.Find("Text").GetComponent<TextMesh>();
        bottomColorBlind = downSticker.transform.Find("Text").GetComponent<TextMesh>();
    }


    void GetEdgework()
    {
        serialNumber = Bomb.GetSerialNumber().ToUpper();

        batteryCount = Bomb.GetBatteryCount();
        holderCount = Bomb.GetBatteryHolderCount();
        aaBatteryCount = 2 * (batteryCount - holderCount);
        dBatteryCount = 2 * holderCount - batteryCount;

        litIndicatorNum = Bomb.GetOnIndicators().Count();
        unlitIndicatorNum = Bomb.GetOffIndicators().Count();
         
        portPlates = Bomb.GetPortPlateCount();

        digitsInSerialNum = serialNumber.Where(c => Char.IsDigit(c)).Count();
        lettersInSerialNum = serialNumber.Length - digitsInSerialNum;
        constantsInSerialNum = Bomb.GetSerialNumberLetters().Where(c => !IsVowel(c)).Count();
    }

    void SetCube()
    {
        if (debug)
        {
            row = 3;
            col = 1;
        }

        else
        {
            row = Rnd.Range(0, 10);
            col = Rnd.Range(0, 10);
        }
     

        topFace = grid[row, col];
        frontFace = grid[(row + 1) % 10, col];
        leftFace = grid[row, col - 1 < 0 ? 9 : col - 1];
        backFace = grid[row - 1 < 0 ? 9 : row - 1, col];
        rightFace = grid[row, (col + 1) % 10];
        bottomFace = grid[(row + 2) % 10, col];


        upSticker.material = colorDictionary[topFace];
        leftSticker.material = colorDictionary[leftFace];
        frontSticker.material = colorDictionary[frontFace];
        rightSticker.material = colorDictionary[rightFace];
        downSticker.material = colorDictionary[bottomFace];
        backSticker.material = colorDictionary[backFace];

        if (GetComponent<KMColorblindMode>().ColorblindModeActive)
        {
            UpdateColorBlindText(upSticker.material, upColorBlind);
            UpdateColorBlindText(leftSticker.material, leftColorBlind);
            UpdateColorBlindText(frontSticker.material, frontColorBlind);
            UpdateColorBlindText(backSticker.material, backColorBlind);
            UpdateColorBlindText(downSticker.material, bottomColorBlind);
            UpdateColorBlindText(rightSticker.material, rightColorBlind);
        }

        else
        {
            upColorBlind.text = leftColorBlind.text = frontColorBlind.text = rightColorBlind.text = backColorBlind.text = bottomColorBlind.text = "";
        }

        Logging($"Top Face is located in {row},{col} (row, col) index 0");
    }

    void UpdateColorBlindText(Material m, TextMesh t)
    {
        KeyValuePair<Color, string> kv = GetColorBlindVariables(m);

        t.color = kv.Key;
        t.text = kv.Value;
    }

    KeyValuePair<Color, string> GetColorBlindVariables (Material m)
    {
        Color c;

        string material = m.name.Substring(0, m.name.Length - 21);
        
        if (material == "Red" || material == "Blue")
        {
            c = new Color(1, 1, 1);
        }

        else
        { 
            c = new Color(0, 0, 0);
        }

        string s = "" + material[0];

        return new KeyValuePair<Color, string>(c, s);
    }

    void GetAnswer()
    {
        List<int> nums = new List<int>();

        for (int i = 0; i < 6; i++)
        {
            char c = serialNumber[i];
            int edgeworkNum = Char.IsDigit(c) ? c - 48 : c - 64;
            int modifier = i / 3 == 0 ? row : col;
            int sum = edgeworkNum + modifier;

            nums.Add(sum);
        }

        string[] arr = nums.Select(x => "" + x.ToString()).ToArray();

        Logging($"Starting numbers {string.Join(", ", arr)}");


        nums[0] = GetTopNewNumber(nums[0]);
        nums[1] = GetLeftNewNumber(nums[1]);
        nums[2] = GetFrontNewNumber(nums[2]);
        nums[3] = GetRightNewNumber(nums[3]);
        nums[4] = GetBottomNewNumber(nums[4]);

        arr = nums.Select(x => "" + x.ToString()).ToArray();

        Logging($"Modified numbers are {string.Join(", ", arr)}");

        answer = new List<string>();

        for (int i = 0; i < 6; i++)
        {
            int n = nums[i];

            while (n < 0)
            {
                n += 6;
            }

            n %= 6;

            switch (n)
            {
                case 0:
                    answer.Add("U");
                    break;
                case 1:
                    answer.Add("L");
                    break;

                case 2:
                    answer.Add("R");
                    break;

                case 3:
                    answer.Add("D");
                    break;

                case 4:
                    answer.Add("C");
                    break;

                case 5:
                    answer.Add("CC");
                    break;
            }
        }

        Logging($"Answer is {string.Join(", ", answer.ToArray())}");
    }

    int GetTopNewNumber(int oldNum)
    {
        int newNum;
        string log = "Top Face is ";
        if (topFace == g)
        {
            newNum = oldNum + aaBatteryCount;
            log += "green. Adding AA battery count.";
        }

        else if (topFace == b)
        {
            newNum = oldNum + dBatteryCount;
            log += "blue. Adding D battery count.";
        }

        else if(topFace == r)
        {
            newNum = oldNum + batteryCount;
            log += "red. Adding battery count.";
        }

        else if (topFace == w)
        {
            newNum = oldNum - aaBatteryCount;
            log += "white. Subtracting AA battery count.";
        }

        else if(topFace == y)
        {
            newNum = oldNum - dBatteryCount;
            log += "yellow. Subtracting D battery count.";
        }

        else
        {
            newNum = oldNum - batteryCount;
            log += "oranage. Subtracting battery count.";
        }

        Logging(log);

        return newNum;
    }
    int GetLeftNewNumber(int oldNum)
    {
        int newNum;
        string log = "Left Face is ";

        if (leftFace == g)
        {
            newNum = oldNum + Bomb.GetPorts().Count(x => x.ToUpper() == "STEREORCA");
            log += "green. Adding Stereo RCA ports.";
        }

        else if (leftFace == b)
        {
            newNum = oldNum + Bomb.GetPorts().Count(x => x.ToUpper() == "SERIAL");
            log += "blue. Adding Serial ports.";

        }

        else if (leftFace == r)
        {
            newNum = oldNum + Bomb.GetPorts().Count(x => x.ToUpper() == "DVI");
            log += "red. Adding DVI-D ports.";
        }

        else if (leftFace == w)
        {
            newNum = oldNum + Bomb.GetPorts().Count(x => x.ToUpper() == "PS2");
            log += "white. Adding PS/2 ports.";
        }

        else if (leftFace == y)
        {
            newNum = oldNum + Bomb.GetPorts().Count(x => x.ToUpper() == "PARALLEL");
            log += "yellow. Adding Parallel ports.";
        }

        else
        { 
            newNum = oldNum + Bomb.GetPorts().Count(x => x.ToUpper() == "RJ45");
            log += "orange. Adding RJ-45 ports.";
        }

        Logging(log);

        return newNum;
    }
    int GetFrontNewNumber(int oldNum)
    {
        int newNum;
        string log = "Front Face is ";
        if (frontFace == g)
        {
            newNum = oldNum + litIndicatorNum;
            log += "green. Adding lit indicators.";

        }

        else if (frontFace == b)
        {
            newNum = oldNum - unlitIndicatorNum;
            log += "blue. Subtracting unlit indicators.";
        }

        else if (frontFace == r)
        {
            newNum = oldNum - litIndicatorNum;
            log += "red. Subtracting lit indicators.";
        }

        else if (frontFace == w)
        {
            newNum = oldNum + unlitIndicatorNum;
            log += "white. Adding unlit indicators.";
        }

        else if (frontFace == y)
        {
            newNum = oldNum * unlitIndicatorNum;
            log += "yellow. Mulitplying unlit indicators.";
        }

        else
        {
            newNum = oldNum * litIndicatorNum; ;
            log += "orange. Mulitplying unlit indicators.";
        }


        Logging(log);
        return newNum;
    }
    int GetRightNewNumber(int oldNum)
    {
        int newNum;
        string log = "Right Face is ";

        if (rightFace == g)
        {
            newNum = oldNum + digitsInSerialNum;
            log += "green. Adding digits in serial number.";
        }

        else if (rightFace == b)
        {
            newNum = oldNum + constantsInSerialNum;
            log += "blue. Adding consonants in serial number.";
        }

        else if (rightFace == r)
        {
            newNum = oldNum + lettersInSerialNum;
            log += "red. Adding letters in serial number.";
        }

        else if (rightFace == w)
        {
            newNum = oldNum - lettersInSerialNum;
            log += "white. Subtracting letters in serial number.";
        }

        else if (rightFace == y)
        {
            newNum = oldNum - digitsInSerialNum;
            log += "yellow. Subtracting digits in serial number.";
        }

        else
        { 
            newNum = oldNum + (lettersInSerialNum - constantsInSerialNum);
            log += "orange. Adding vowels in serial number.";
        }

        Logging(log);
        return newNum;
    }
    int GetBottomNewNumber(int oldNum)
    {
        int newNum;
        string log = "Down Face is ";
        if (bottomFace == g)
        {
            newNum = oldNum - portPlates;
            log += "green. Subtracting port plates";
        }

        else if (bottomFace == b)
        {
            newNum = oldNum + 4;
            log += "blue. Adding 4.";
        }

        else if(bottomFace == r)
        {
            newNum = oldNum + portPlates;
            log += "red. Adding port plates.";
        }

        else if(bottomFace == w)
        {
            newNum = oldNum * portPlates;
            log += "white. Multiplying port plates.";
        }

        else if(bottomFace == y)
        {
            newNum = oldNum * 2;
            log += "yellow. Multiplying 2.";
        }

        else
        {
            newNum = oldNum + 8;
            log += "orange. Adding 8.";
        }

        Logging(log);
        return newNum;
    }

    bool IsVowel(char c)
    {
        return "AEIOU".IndexOf(c) > -1;
    }
    void Logging(string log)
    {
        Debug.LogFormat($"[1x1x1 Rubik's Cube #{ModuleId}] {log}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use `!{0}` followed by U, L, R, D, C, or CC to rotate the cube in their respective directions. Moves can be chained with a space between them. Prepend S to start the timer. Having the S anywhere besides the start will make the command invalid. Use `!{0} Reset` to press the reset button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        Command = Command.ToUpper().Trim();
        yield return null;

        string[] commands = Command.Split(' ');

        if (commands[0] == "RESET")
        {
            if (commands.Length == 1)
            {
                resetButton.OnInteract();
            }

            else
            {
                yield return "sendtochaterror Too many commands.";
            }
        }

        else if (commands[0] == "S")
        {
            string[] moves = new string[commands.Length - 1];

            for (int i = 0; i < moves.Length; i++)
            {
                moves[i] = commands[i + 1];
            }

            if (ValidCommand(moves))
            {
                yield return StartButton();
                yield return HandleMoves(moves);
            }

            else
            {
                yield return "sendtochaterror Invalid move command.";
                yield break;
            }
        }

        else
        {
            if (ValidCommand(commands))
            {
                yield return HandleMoves(commands);
            }

            else
            {
                yield return "sendtochaterror Invalid move command.";
            }
        }

        while (!ModuleSolved)
        {
            yield return null;
        }


    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return ProcessTwitchCommand($"S {string.Join(" ", answer.ToArray())}");
    }

    IEnumerator HandleMoves(string[] moves)
    {
        int currentStrikeNum = Bomb.GetStrikes();

        foreach (string m in moves)
        {
            switch (m)
            {
                case "U":
                    yield return Rotate(Rotation.Up, inputTime);
                    break;

                case "L":
                    yield return Rotate(Rotation.Left, inputTime);
                    break;

                case "R":
                    yield return Rotate(Rotation.Right, inputTime);
                    break;

                case "D":
                    yield return Rotate(Rotation.Down, inputTime);
                    break;

                case "C":
                    yield return Rotate(Rotation.Clock, inputTime);
                    break;

                default: //CC
                    yield return Rotate(Rotation.Counter, inputTime);
                    break;
            } 


            if (Bomb.GetStrikes() > currentStrikeNum)
            {
                yield break;
            }
        }
    }


    bool ValidCommand(string[] s)
    {
        return s.Count(x => !new string[] { "U", "L", "R", "D", "C", "CC" }.Contains(x)) == 0;
    }
}