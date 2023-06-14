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

    private KMSelectable upButton, leftButton, rightButton, downButton, clockButton, counterButton, startButton;

    private const float maxTime = 10f;
    private float currentTime;
    private TextMesh timerText;

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

    private int aaBatteryCount, batteries, dBatteryCount, litIndicatorNum, unlitIndicatorNum, digitsInSerialNum, constantsInSerialNum, lettersInSerialNum, portPlates;

    private string serialNumber;

    private List<string> answer;
    private List<string> inputList;


    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    private bool timerStarted = false;



    enum Rotation
    { 
        Up,
        Left,
        Right,
        Down,
        Clock,
        Counter
    }
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

        upButton.OnInteract += delegate () { StartCoroutine(Rotate(Rotation.Up)); return false; };
        downButton.OnInteract += delegate () {  StartCoroutine(Rotate(Rotation.Down)); return false; };

        leftButton.OnInteract += delegate () {  StartCoroutine(Rotate(Rotation.Left)); return false; };
        rightButton.OnInteract += delegate () { StartCoroutine(Rotate(Rotation.Right)); return false; };

        clockButton.OnInteract += delegate () { StartCoroutine(Rotate(Rotation.Clock)); return false; };
        counterButton.OnInteract += delegate () { StartCoroutine(Rotate(Rotation.Counter)); return false; };
        startButton.OnInteract += delegate () { timerStarted = true;  return false; };
    }

    void Start()
    {
        SetCube(); 
        GetEdgework();
        GetAnswer();
        ResetModule();
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

    IEnumerator Rotate(Rotation rotation)
    {
        if (rotating || ModuleSolved)
            yield break;

        rotating = true;

        const float maxTime = .5f; //how much time the rotation should take 

        Quaternion initialRotation = cube.transform.rotation;
        Vector3Int initialRotationVector = new Vector3Int((int)initialRotation.x, (int)initialRotation.y, (int)initialRotation.z);


        Vector3 axis; 
        switch (rotation)
        {
            case Rotation.Up:
                axis = cube.transform.right;
                break;
            case Rotation.Down:
                axis = -cube.transform.right;
                break;
            case Rotation.Right:
                axis = cube.transform.forward;
                break;
            case Rotation.Left:
                axis = -cube.transform.forward;
                break;
            case Rotation.Clock:
                axis = cube.transform.up;
                break;
            default: //counter
                axis = -cube.transform.up;
                break;
        }

        axis *= 90;

        Vector3Int axisInt = new Vector3Int((int)axis.x, (int)axis.y, (int)axis.z);

        Vector3Int finalRotation = initialRotationVector + axisInt;

        Debug.Log("Initial rotation: " + initialRotationVector);
        Debug.Log("Final rotation: " + finalRotation);

        float timer = 0f;
        do
        {
            yield return null;
            cube.transform.rotation = initialRotation;
            cube.transform.Rotate(axis * Math.Min(timer/maxTime, maxTime));
            timer += Time.deltaTime;
        } while (timer < maxTime);

        cube.transform.eulerAngles = finalRotation;

        if (timerStarted)
        { 
            HandleInput(rotation);
        }

        rotating = false;
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

        timerText = transform.Find("Timer").transform.Find("Text").GetComponent<TextMesh>();

        upSticker = cube.transform.Find("Up Sticker").GetComponent<MeshRenderer>();
        leftSticker = cube.transform.Find("Left Sticker").GetComponent<MeshRenderer>();
        frontSticker = cube.transform.Find("Front Sticker").GetComponent<MeshRenderer>();
        rightSticker = cube.transform.Find("Right Sticker").GetComponent<MeshRenderer>();
        backSticker = cube.transform.Find("Back Sticker").GetComponent<MeshRenderer>();
        downSticker = cube.transform.Find("Down Sticker").GetComponent<MeshRenderer>();
    }

    void GetEdgework()
    {
        serialNumber = Bomb.GetSerialNumber().ToUpper();

        batteries = Bomb.GetBatteryCount();
        aaBatteryCount = GetAABatteryCount();
        dBatteryCount = batteries - aaBatteryCount;

        litIndicatorNum = Bomb.GetOnIndicators().Count();
        unlitIndicatorNum = Bomb.GetOffIndicators().Count();

        portPlates = Bomb.GetPortPlateCount();

        digitsInSerialNum = serialNumber.Where(c => Char.IsDigit(c)).Count();
        lettersInSerialNum = serialNumber.Length - digitsInSerialNum;
        constantsInSerialNum = serialNumber.Where(c => !IsVowel(c)).Count();
    }

    void SetCube()
    {
        row = Rnd.Range(0, 10);
        col = Rnd.Range(0, 10);

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

        Logging($"Top Face is located in {row},{col} (row, col) index 0");
    }

    void GetAnswer()
    {
        List<int> nums = new List<int>();

        foreach (char c in serialNumber)
        {
            nums.Add(Char.IsDigit(c) ? c - 48 : c - 64);        
        }

        nums[0] = GetTopNewNumber(nums[0]);
        nums[1] = GetLeftNewNumber(nums[1]);
        nums[2] = GetFrontNewNumber(nums[2]);
        nums[3] = GetRightNewNumber(nums[3]);
        nums[4] = GetBottomNewNumber(nums[4]);

        string[] arr = nums.Select(x => "" + x.ToString()).ToArray();

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
            newNum = oldNum + batteries;
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
            newNum = oldNum - batteries;
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
            newNum = oldNum + Bomb.GetPorts().Where(x => x == "Stereo RCA").Count();
            log += "green. Adding Stereo RCA ports.";
        }

        else if (leftFace == b)
        {
            newNum = oldNum + Bomb.GetPorts().Where(x => x == "Serial").Count();
            log += "green. Adding Serial ports.";

        }

        else if (leftFace == r)
        {
            newNum = oldNum + Bomb.GetPorts().Where(x => x == "DVI-D").Count();
            log += "green. Adding DVI-D ports.";
        }

        else if (leftFace == w)
        {
            newNum = oldNum + Bomb.GetPorts().Where(x => x == "PS/2").Count();
            log += "green. Adding PS/2 ports.";
        }

        else if (leftFace == y)
        {
            newNum = oldNum + Bomb.GetPorts().Where(x => x == "Parallel").Count();
            log += "green. Adding Parallel ports.";
        }

        else
        { 
            newNum = oldNum + Bomb.GetPorts().Where(x => x == "RJ-45").Count(); //orange
            log += "green. Adding RJ-45 ports.";
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
            newNum = oldNum + unlitIndicatorNum;
            log += "green. Adding unlit indicators.";
        }

        else if (frontFace == r)
        {
            newNum = oldNum - litIndicatorNum;
            log += "green. Subtracting lit indicators.";
        }

        else if (frontFace == w)
        {
            newNum = oldNum + unlitIndicatorNum;
            log += "green. Subtracting unlit indicators.";
        }

        else if (frontFace == y)
        {
            newNum = oldNum * unlitIndicatorNum;
            log += "green. Mulitplying unlit indicators.";
        }

        else
        {
            newNum = oldNum * litIndicatorNum; ; //orange
            log += "green. Mulitplying unlit indicators.";
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
            log += "blue. Adding constants in serial number.";
        }

        else if (rightFace == r)
        {
            newNum = lettersInSerialNum;
            log += "red. Adding letters in serial number.";
        }

        else if (rightFace == w)
        {
            newNum = oldNum - digitsInSerialNum;
            log += "white. Subtracting digits in serial number.";
        }

        else if (rightFace == y)
        {
            newNum = oldNum - digitsInSerialNum;
            log += "yellow. Subtracting digits in serial number.";
        }

        else
        { 
            newNum = lettersInSerialNum - constantsInSerialNum;
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

        if (bottomFace == b)
        {
            newNum = oldNum + 4;
            log += "blue. Adding 4.";
        }

        if (bottomFace == r)
        {
            newNum = oldNum - portPlates;
            log += "red. Subtracting port plates.";
        }

        if (bottomFace == w)
        {
            newNum = oldNum * portPlates;
            log += "white. Multiplying port plates.";
        }

        if (bottomFace == y)
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

    int GetAABatteryCount()
    {
        int count = 0;
        int tempBatteries = batteries;

        while (tempBatteries > 2)
        {
            tempBatteries -= 2;
            count++;
        }

        return count * 2;
    }

    bool IsVowel(char c)
    {
        return "AEIOU".IndexOf(c) > -1;
    }
    void Logging(string log)
    {
        Debug.LogFormat($"[1x1x1 Rubik’s Cube #{ModuleId}] {log}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}