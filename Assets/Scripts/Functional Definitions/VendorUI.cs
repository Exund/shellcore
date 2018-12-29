﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IVendor
{
    VendingBlueprint GetVendingBlueprint();
}

public class VendorUI : MonoBehaviour, IDialogueable
{
    /*[System.Serializable]
    public struct Item
    {
        public EntityBlueprint turretBlueprint; //TODO: replace this with blueprint
        public Sprite icon;
        public int cost;
    }*/

    //public List<Item> items;
    public VendingBlueprint blueprint;
    public GameObject UIPrefab;
    public GameObject buttonPrefab;
    public PlayerCore player;
    public Vector3 outpostPosition;
    private GameObject UI;
    private Transform background;
    private bool opened;
    private GameObject[] buttons;

    public void openUI()
    {
        if (UI)
            return;
        if (!blueprint)
        {
            Debug.Log("No blueprint!");
            return;
        }
        if (!UIPrefab)
        {
            UIPrefab = ResourceManager.GetAsset<GameObject>("vendor_ui");
        }
        if (!buttonPrefab)
        {
            buttonPrefab = ResourceManager.GetAsset<GameObject>("vendor_button");
        }

        UI = Instantiate(UIPrefab);
        background = UI.transform.Find("Background");
        Button close = background.transform.Find("Close").GetComponent<Button>();
        close.onClick.AddListener(closeUI);
        buttons = new GameObject[blueprint.items.Count];
        for (int i = 0; i < blueprint.items.Count; i++)
        {
            int index = i;
            buttons[i] = Instantiate(buttonPrefab);

            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            rt.SetParent(background, false);
            rt.anchoredPosition = new Vector2(16 + i * 64, -16 + (i > 6 ? 96 : 0));

            Button button = buttons[i].GetComponent<Button>();
            button.onClick.AddListener(() => { onButtonPressed(index); });

            Image sr = buttons[i].transform.Find("Icon").GetComponent<Image>();
            sr.sprite = blueprint.items[i].icon;
        }
        opened = true;
    }

    private void Update()
    {
        if (opened)
        {
            for (int i = 0; i < blueprint.items.Count; i++)
            {
                if(player.GetPower() < blueprint.items[i].cost)
                {
                    buttons[i].GetComponent<Image>().color = new Color(0, 0, 0.4F);
                } else buttons[i].GetComponent<Image>().color = Color.white;
            }
        } 
    }

    public void closeUI()
    {
        opened = false;
        Destroy(UI);
    }

    public void onButtonPressed(int index)
    {
        //TODO: construct entity from blueprint
        //TODO: add sprites and all necessary prefab IDs to the blueprint
        if (player.GetPower() >= blueprint.items[index].cost)
        {
            GameObject creation = new GameObject();
            switch(blueprint.items[index].entityBlueprint.intendedType)
            {
                case EntityBlueprint.IntendedType.Turret:
                    Turret tur = creation.AddComponent<Turret>();
                    tur.blueprint = blueprint.items[index].entityBlueprint;
                    tur.SetOwner(player);
                    break;
                case EntityBlueprint.IntendedType.Tank:
                    Tank tank = creation.AddComponent<Tank>();
                    tank.blueprint = blueprint.items[index].entityBlueprint;
                    tank.enginePower = 250;
                    break;
                default:
                    break;
            }

            creation.transform.position = outpostPosition;
            creation.GetComponent<Entity>().spawnPoint = outpostPosition;
            player.SetTractorTarget(creation.GetComponent<Draggable>());
            player.AddPower(-blueprint.items[index].cost);
            closeUI();
        }
    }
}