﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public interface IVendor
{
    VendingBlueprint GetVendingBlueprint();
    int GetFaction();
    Vector3 GetPosition();
}

public class VendorUI : MonoBehaviour, IDialogueable, IWindow
{
    public VendingBlueprint blueprint;
    public GameObject UIPrefab;
    public GameObject buttonPrefab;
    public PlayerCore player;
    private IVendor vendor;
    private GameObject UI;
    private Transform background;
    private bool opened;
    private GameObject[] buttons;
    private Text costInfo;
    private Text nameInfo;
    public int range;
    public GameObject tooltipPrefab;
    public static VendorUI instance;

    public bool GetActive()
    {
        return UI && UI.activeSelf;
    }

    public void openUI()
    {
        if (opened)
        {
            var vendor = this.vendor;
            var player = this.player;
            CloseUI();
            SetVendor(vendor, player);
        }

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
        UI.GetComponentInChildren<GUIWindowScripts>().Activate();
        instance = this;
        background = UI.transform.Find("Container").Find("Background");
        Button close = background.transform.Find("Close").GetComponent<Button>();
        close.onClick.AddListener(CloseUI);
        costInfo = background.transform.Find("Cost").GetComponent<Text>();
        nameInfo = background.transform.Find("Name").GetComponent<Text>();
        costInfo.text = "";
        range = blueprint.range;


        buttons = new GameObject[blueprint.items.Count];
        for (int i = 0; i < blueprint.items.Count; i++)
        {
            int index = i;
            var buttonGO = buttons[i] = Instantiate(buttonPrefab);
            var item = blueprint.items[i];

            RectTransform rt = buttonGO.GetComponent<RectTransform>();
            rt.SetParent(background, false);
            rt.anchoredPosition = new Vector2(185 + (i % 5) * 64, -40 - (i > 4 ? 64 : 0));

            Button button = buttonGO.GetComponent<Button>();
            button.onClick.AddListener(() => { onButtonPressed(index); });

            VendorUIButton vendorUIButton = buttonGO.GetComponent<VendorUIButton>();

            if (player.GetPower() < item.cost)
            {
                buttonGO.GetComponent<Image>().color = new Color(0, 0, 0.4F);
            }

            vendorUIButton.blueprint = item.entityBlueprint;
            vendorUIButton.costText = $"POWER COST: <color=cyan>{item.cost}</color>";
            vendorUIButton.descriptionText = item.description;
            vendorUIButton.tooltipPrefab = tooltipPrefab;
            vendorUIButton.costInfo = costInfo;
            vendorUIButton.nameInfo = nameInfo;
            vendorUIButton.handler = UI.GetComponentInChildren<SelectionDisplayHandler>();

            Image sr = buttonGO.transform.Find("Icon").GetComponent<Image>();
            sr.sprite = item.icon;

            Text[] texts = buttonGO.GetComponentsInChildren<Text>();
            texts[0].text = (i + 1).ToString();
            texts[1].text = item.cost.ToString();
            texts[1].color = Color.cyan;
        }

        opened = true;
    }

    public void SetVendor(IVendor vendor, PlayerCore player)
    {
        this.player = player;
        this.vendor = vendor;
        blueprint = vendor.GetVendingBlueprint();
    }

    private void ClearVendor()
    {
        this.player = null;
        this.vendor = null;
        this.blueprint = null;
    }

    private void Update()
    {
        if (opened)
        {
            if (vendor == null)
            {
                Debug.LogWarning("No vendor!");
                return;
            }

            if (player == null)
            {
                Debug.Log("No player set! Using default player.");
                player = PlayerCore.Instance;
            }

            if (player && vendor.GetFaction() != player.faction)
            {
                Debug.Log("Vendor faction changed");
                CloseUI();
                return;
            }

            if ((vendor.GetPosition() - player.transform.position).sqrMagnitude > range)
            {
                Debug.Log("Player moved out of the vendor range");
                CloseUI();
                return;
            }

            for (int i = 0; i < blueprint.items.Count; i++)
            {
                var image = buttons[i].GetComponent<Image>();
                if (player.GetPower() < blueprint.items[i].cost)
                {
                    image.color = new Color(0, 0, 0.4F);
                }
                else
                {
                    image.color = Color.white;
                }
            }
        }
    }

    public void CloseUI()
    {
        UI.GetComponentInChildren<GUIWindowScripts>().CloseUI();
        opened = false;
        Destroy(UI);
        ClearVendor();
    }

    public static Entity BuyItem(ShellCore core, int index, IVendor vendor)
    {
        if (core.unitsCommanding.Count >= core.GetTotalCommandLimit())
        {
            return null;
        }

        GameObject creation = new GameObject();
        creation.transform.position = vendor.GetPosition();
        var item = vendor.GetVendingBlueprint().items[index];
        var entityBlueprint = item.entityBlueprint;
        switch (entityBlueprint.intendedType)
        {
            case EntityBlueprint.IntendedType.Turret:
                Turret tur = creation.AddComponent<Turret>();
                tur.blueprint = entityBlueprint;
                core.SetTractorTarget(creation.GetComponent<Draggable>());
                tur.SetOwner(core);

                if (SectorManager.instance)
                {
                    var BZM = SectorManager.instance.GetComponentInChildren<BattleZoneManager>();
                    if (BZM)
                    {
                        var stats = BZM.stats.Find(s => s.faction == core.faction);
                        if (stats == null)
                        {
                            stats = new BattleZoneManager.Stats(core.faction);
                            BZM.stats.Add(stats);
                        }

                        stats.turretSpawns++;
                    }
                }
                break;
            case EntityBlueprint.IntendedType.Tank:
                Tank tank = creation.AddComponent<Tank>();
                tank.blueprint = entityBlueprint;
                tank.SetOwner(core);
                break;
            default:
                break;
        }

        creation.GetComponent<Entity>().spawnPoint = vendor.GetPosition();
        creation.GetComponent<Entity>().faction = core.faction;
        creation.name = entityBlueprint.name;
        core.sectorMngr.InsertPersistentObject(entityBlueprint.name, creation);
        core.AddPower(-item.cost);
        return creation.GetComponent<Entity>();
    }

    public void onButtonPressed(int index)
    {
        if (player.GetPower() >= blueprint.items[index].cost && FactionManager.IsAllied(player.faction, vendor.GetFaction())
                                                             && player.unitsCommanding.Count < player.GetTotalCommandLimit())
        {
            BuyItem(player, index, vendor);
            if (GetActive())
            {
                CloseUI();
            }

            ClearVendor();
        }
        else if (player && player.GetUnitsCommanding().Count >= player.GetTotalCommandLimit())
        {
            player.alerter.showMessage("Unit limit reached!", "clip_alert");
        }
    }

    public UnityEvent GetOnCancelled()
    {
        return null;
    }
}
