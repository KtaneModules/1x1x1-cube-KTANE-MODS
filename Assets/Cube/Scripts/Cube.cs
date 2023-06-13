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
    private GameObject upSticker, leftSticker, frontSticker, rightSticker, downSticker, backSticker;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;


    bool rotating = false;


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


        Vector3 axis; 
        switch (rotation)
        {
            case Rotation.Up:
                axis = Vector3.right;
                break;
            case Rotation.Down:
                axis = Vector3.left;
                break;
            case Rotation.Right:
                axis = Vector3.back;
                break;
            case Rotation.Left:
                axis = Vector3.forward;
                break;
            case Rotation.Clock:
                axis = Vector3.up;
                break;
            default: //counter
                axis = Vector3.down;
                break;
        }

        axis *= 90;
        Vector3Int finalRotation = new Vector3Int((int)initialRotation.x, (int)initialRotation.y, (int)initialRotation.z) + new Vector3Int((int)axis.x, (int)axis.y, (int)axis.z);
        Debug.Log("Initial rotation: " + initialRotation);
        Debug.Log("Final rotation: " + finalRotation);

        float timer = 0f;
        do
        {
            yield return null;
            cube.transform.rotation = initialRotation;
            cube.transform.Rotate(axis * Math.Min(timer/maxTime, 1));
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