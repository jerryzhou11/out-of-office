using System;
using UnityEngine;
using UnityEngine.InputSystem;

//https://www.youtube.com/watch?v=a1RFxtuTVsk
public class TutorialManager : MonoBehaviour
{
    public GameObject[] popUps;
    private int popUpIndex;
    public GameObject Spawner;

    void Update()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        Animator anim = pc.GetComponent<Animator>();
    
        for (int i = 0; i < popUps.Length; i++)
        {
            if (i == popUpIndex)
            {
                popUps[i].SetActive(true);
            }
            else
            {
                popUps[i].SetActive(false);
            }
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        pc.DisableAttack(5f);

        if (anim != null)
        {
            anim.SetBool("IsTut", true);
        }

        print(popUpIndex);

        if (keyboard != null)
        {
            if (popUpIndex == 1) { 
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed 
                    || keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed
                    || keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed
                    || keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    popUpIndex++;
                }
            }
        }

        if (mouse != null)
        {
            if (popUpIndex == 1) 
            {
                popUpIndex++;
            } 
        }      
    }
}
