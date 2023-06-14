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


    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;



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

        upButton.OnInteract += delegate () { Debug.Log("Pressed up"); StartCoroutine(Rotate(Rotation.Up)); return false; };
        downButton.OnInteract += delegate () { Debug.Log("Pressed up"); StartCoroutine(Rotate(Rotation.Down)); return false; };

        leftButton.OnInteract += delegate () { Debug.Log("Pressed left"); StartCoroutine(Rotate(Rotation.Left)); return false; };
        rightButton.OnInteract += delegate () { Debug.Log("Pressed right"); StartCoroutine(Rotate(Rotation.Right)); return false; };

        clockButton.OnInteract += delegate () { Debug.Log("Pressed right"); StartCoroutine(Rotate(Rotation.Clock)); return false; };
        counterButton.OnInteract += delegate () { Debug.Log("Pressed right"); StartCoroutine(Rotate(Rotation.Counter)); return false; }; 
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */

        //button.OnInteract += delegate () { buttonPress(); return false; };



    }

    void Start()
    {
        SetCube();
    }

    void Update()
    {

    }

    IEnumerator Rotate(Rotation rotation)
    {
        if (rotating)
            yield break;

        rotating = true;

        const float maxTime = 1f; //how much time the rotation should take 

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

        rotating = false;
        cube.transform.eulerAngles = finalRotation;
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

        upSticker = cube.transform.Find("Up Sticker").GetComponent<MeshRenderer>();
        leftSticker = cube.transform.Find("Left Sticker").GetComponent<MeshRenderer>();
        frontSticker = cube.transform.Find("Front Sticker").GetComponent<MeshRenderer>();
        rightSticker = cube.transform.Find("Right Sticker").GetComponent<MeshRenderer>();
        backSticker = cube.transform.Find("Back Sticker").GetComponent<MeshRenderer>();
        downSticker = cube.transform.Find("Down Sticker").GetComponent<MeshRenderer>();
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

        Logging($"Top color is {upSticker.material.name}");
        Logging($"Left color is {leftSticker.material.name}");
        Logging($"Front color is {frontSticker.material.name}");
        Logging($"Right color is {rightSticker.material.name}");
        Logging($"Down color is {downSticker.material.name}");
        Logging($"Back color is {backSticker.material.name}");

        Logging($"Top Face is located in {row},{col} (row, col) index 0");
    }

    void GetAnswer()
    {
        string serialNumber = Bomb.GetSerialNumber().ToUpper();

        List<int> nums = new List<int>();

        foreach (char c in serialNumber)
        {
            nums.Add(Char.IsDigit(c) ? c - 48 : c - 64);        
        }
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