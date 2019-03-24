﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipBuilder : MonoBehaviour, IWindow {
	public GameObject SBPrefab;
	public Vector3 yardPosition;
	public Image shell;
	public Image core;
	public ShipBuilderCursorScript cursorScript;
	public GameObject buttonPrefab;
	public PlayerCore player;
	public Transform smallContents;
	public Transform mediumContents;
	public Transform largeContents;
	public Transform traderSmallContents;
	public Transform traderMediumContents;
	public Transform traderLargeContents;
	private Transform[] contentsArray; // holds scroll view sub-sections by part size
	private Transform[] traderContentsArray;
	public GameObject smallText;
	public GameObject mediumText;
	public GameObject largeText;
	public GameObject traderSmallText;
	public GameObject traderMediumText;
	public GameObject traderLargeText;
	private GameObject[] contentTexts;
	private GameObject[] traderContentTexts;
	public PresetButton[] presetButtons;
	private string searcherString;
	private bool[] displayingTypes;
	public Image reconstructImage;
	public Text reconstructText;
	bool initialized;
	public TipsFromTheYard tips;
	public GameObject traderScrollView;
	Dictionary<EntityBlueprint.PartInfo, ShipBuilderInventoryScript> partDict;
	Dictionary<EntityBlueprint.PartInfo, ShipBuilderInventoryScript> traderPartDict;
	public BuilderMode mode;

	public bool DecrementPartButton(EntityBlueprint.PartInfo info) {
		if(partDict.ContainsKey(CullSpatialValues(info)) && partDict[CullSpatialValues(info)].GetCount() > 0) {
			partDict[CullSpatialValues(info)].DecrementCount();
			return true;
		} else return false;
	}
	public static EntityBlueprint.PartInfo CullSpatialValues(EntityBlueprint.PartInfo partToCull) {
		var part = new EntityBlueprint.PartInfo();
		part.partID = partToCull.partID;
		part.secondaryData = partToCull.secondaryData;
		part.abilityID = partToCull.abilityID;
		part.tier = partToCull.tier;
		return part;
	}
	public enum TransferMode {
		Sell,
		Return,
		Buy
	}
	public void DispatchPart(ShipBuilderPart part, TransferMode transferMode) {
		var culledInfo = CullSpatialValues(part.info);
		Dictionary<EntityBlueprint.PartInfo, ShipBuilderInventoryScript> dict;
		Transform[] dictContentsArray;
		GameObject[] dictContentTexts;
		switch(transferMode) {
			case TransferMode.Sell:
				cursorScript.buildCost -= ResourceManager.GetAsset<PartBlueprint>(part.info.partID).value;
				dictContentsArray = traderContentsArray;
				dict = traderPartDict;
				dictContentTexts = traderContentTexts;
				break;
			case TransferMode.Buy:
				dictContentsArray = contentsArray;
				dict = partDict;
				dictContentTexts = contentTexts;
				break;
			default: // transfer back to original inventory
				if(part.mode == BuilderMode.Trader) cursorScript.buildCost 
					-= ResourceManager.GetAsset<PartBlueprint>(part.info.partID).value;
				dict = (part.mode == BuilderMode.Yard ? partDict : traderPartDict);
				dictContentsArray = (part.mode == BuilderMode.Yard ? contentsArray : traderContentsArray);
				dictContentTexts = (part.mode == BuilderMode.Yard ? contentTexts : traderContentTexts);
				break;
		}
		if(!dict.ContainsKey(culledInfo)) {
			int size = ResourceManager.GetAsset<PartBlueprint>(part.info.partID).size;
			ShipBuilderInventoryScript dictInvButton = Instantiate(buttonPrefab, 
				dictContentsArray[size]).GetComponent<ShipBuilderInventoryScript>();
			switch(transferMode) { // set the new button's transfer mode
				case TransferMode.Buy:
					dictInvButton.mode = BuilderMode.Yard;
					break;
				case TransferMode.Sell:
					dictInvButton.mode = BuilderMode.Trader;
					break;
				case TransferMode.Return:
					dictInvButton.mode = part.mode;
					break;
			}
			dict.Add(culledInfo, dictInvButton);
			dictContentTexts[size].SetActive(true);
			dict[culledInfo].part = culledInfo;
			dict[culledInfo].cursor = cursorScript;
		}
		dict[culledInfo].IncrementCount();
		cursorScript.buildValue -= ResourceManager.GetAsset<PartBlueprint>(part.info.partID).value;
		cursorScript.parts.Remove(part);
		Destroy(part.gameObject);
	}

	public static Bounds GetRect(RectTransform rectTransform) {
		Bounds rect = RectTransformUtility.CalculateRelativeRectTransformBounds(rectTransform.parent, rectTransform);
		rect.center = rectTransform.anchoredPosition;
		rect.size = rect.size * 0.8F;
		return rect;
	}
	private enum ReconstructButtonStatus {
		Valid,
		PartInvalidPosition,
		NotEnoughCredits
	}
	private void SetReconstructButton(ReconstructButtonStatus status) {
		switch(status) {
			case ReconstructButtonStatus.Valid:
				reconstructImage.color = reconstructText.color = Color.green;
				reconstructText.text = "Reconstruct";
				break;
			case ReconstructButtonStatus.PartInvalidPosition:
				reconstructImage.color = reconstructText.color = Color.red;
				reconstructText.text = "A part is in an invalid position!";
				break;
			case ReconstructButtonStatus.NotEnoughCredits:
				reconstructImage.color = reconstructText.color = Color.red;
				reconstructText.text = "Not enough credits!";
				break;
		}
	}
	void UpdateChainHelper(ShipBuilderPart part) {
		var x = GetRect(part.rectTransform);
		foreach(ShipBuilderPart shipPart in cursorScript.parts) {
			if(!shipPart.isInChain) {
				var y = GetRect(shipPart.rectTransform);
				if(x.Intersects(y)) {
					shipPart.isInChain = true;
					UpdateChainHelper(shipPart);
				}
			}
		}
	}

	public void UpdateChain() {
		SetReconstructButton(cursorScript.buildCost > player.credits ? 
			ReconstructButtonStatus.NotEnoughCredits : ReconstructButtonStatus.Valid);
		var shellRect = GetRect(shell.rectTransform);
		foreach(ShipBuilderPart shipPart in cursorScript.parts) {
			shipPart.isInChain = false;
			var partBounds = GetRect(shipPart.rectTransform);
			if(partBounds.Intersects(shellRect)) {
				bool z = Mathf.Abs(shipPart.rectTransform.anchoredPosition.x - shell.rectTransform.anchoredPosition.x) <
				0.18F*(shipPart.rectTransform.sizeDelta.x + shell.rectTransform.sizeDelta.x) &&
				Mathf.Abs(shipPart.rectTransform.anchoredPosition.y - shell.rectTransform.anchoredPosition.y) <
				0.18F*(shipPart.rectTransform.sizeDelta.y + shell.rectTransform.sizeDelta.y);
				shipPart.isInChain = !z;
			}
		}
		foreach(ShipBuilderPart shipPart in cursorScript.parts) {
			if(shipPart.isInChain) UpdateChainHelper(shipPart);
		}
		foreach(ShipBuilderPart shipPart in cursorScript.parts) { 
			// perform the same calculation again to falsify parts that are too close to the shell
			// TODO: make this less st*pid
			var partBounds = GetRect(shipPart.rectTransform);
			if(partBounds.Intersects(shellRect)) {
				bool z = Mathf.Abs(shipPart.rectTransform.anchoredPosition.x - shell.rectTransform.anchoredPosition.x) <
				0.18F*(shipPart.rectTransform.sizeDelta.x + shell.rectTransform.sizeDelta.x) &&
				Mathf.Abs(shipPart.rectTransform.anchoredPosition.y - shell.rectTransform.anchoredPosition.y) <
				0.18F*(shipPart.rectTransform.sizeDelta.y + shell.rectTransform.sizeDelta.y);
				shipPart.isInChain = !z;
			}
		}
		foreach(ShipBuilderPart shipPart in cursorScript.parts) {
			if(!shipPart.isInChain || !shipPart.validPos) {
				SetReconstructButton(ReconstructButtonStatus.PartInvalidPosition);
				return;
			}
		}
	}
	public void Initialize(BuilderMode mode, List<EntityBlueprint.PartInfo> traderInventory = null) {

		//initialize window on screen
		if(initialized) CloseUI(false); // prevent initializing twice by closing UI if already initialized
		initialized = true;
		PlayerViewScript.SetCurrentWindow(this);
		GetComponent<Canvas>().sortingOrder = ++PlayerViewScript.currentLayer; // move window to top
		cursorScript.gameObject.SetActive(false);

		// set up actual stats
		this.mode = mode;
		searcherString = "";
		contentsArray = new Transform[] {smallContents, mediumContents, largeContents};
		traderContentsArray = new Transform[] {traderSmallContents, traderMediumContents, traderLargeContents};
		contentTexts = new GameObject[] {smallText, mediumText, largeText};
		traderContentTexts = new GameObject[] {traderSmallText, traderMediumText, traderLargeText};
		foreach(GameObject obj in contentTexts) {
			obj.SetActive(false);
		}
		foreach(GameObject traderObj in traderContentTexts) {
			traderObj.SetActive(false);
		}
		displayingTypes = new bool[] {true, true, true, true, true};
		player.SetIsInteracting(true);
		partDict = new Dictionary<EntityBlueprint.PartInfo, ShipBuilderInventoryScript>();
		traderPartDict = new Dictionary<EntityBlueprint.PartInfo, ShipBuilderInventoryScript>();
		shell.sprite = ResourceManager.GetAsset<Sprite>(player.blueprint.coreShellSpriteID);
		shell.color = FactionColors.colors[0];
		shell.rectTransform.sizeDelta = shell.sprite.bounds.size * 100;
		core.sprite = ResourceManager.GetAsset<Sprite>(player.blueprint.coreSpriteID);
		core.material = ResourceManager.GetAsset<Material>("material_color_swap");
		core.color = FactionColors.colors[0];
		core.preserveAspect = true;
		core.rectTransform.sizeDelta = core.sprite.bounds.size * 110;
		List<EntityBlueprint.PartInfo> parts = player.GetInventory();
		cursorScript.player = player;
		cursorScript.handler = player.GetAbilityHandler();

		// hide the buttons and yard tips if interacting with a trader

		tips.gameObject.SetActive(mode == BuilderMode.Yard);
		traderScrollView.gameObject.SetActive(mode == BuilderMode.Trader);

		traderInventory = new List<EntityBlueprint.PartInfo>();
		EntityBlueprint.PartInfo info1 = new EntityBlueprint.PartInfo();
		info1.partID = "BigWing1";
		info1.abilityID = 1;
		info1.tier = 2;
		traderInventory.Add(info1);
		if(traderInventory != null) {
			foreach(EntityBlueprint.PartInfo info in traderInventory) {
				if(!traderPartDict.ContainsKey(info)) {
					int size = ResourceManager.GetAsset<PartBlueprint>(info.partID).size;
					ShipBuilderInventoryScript traderInvButton = Instantiate(buttonPrefab, // instantiate part button for trader
						traderContentsArray[size]).GetComponent<ShipBuilderInventoryScript>();
						traderContentTexts[size].SetActive(true);
						traderInvButton.part = info1;
						traderInvButton.cursor = cursorScript;
						traderInvButton.mode = BuilderMode.Trader;
					traderPartDict.Add(info, traderInvButton);
					for(int i = 0; i < 500; i++) traderInvButton.IncrementCount();
				}
			}
		}

		if(parts != null) {
			if(parts.Count == 0) {
				EntityBlueprint.PartInfo info = new EntityBlueprint.PartInfo();
				foreach(string name in ResourceManager.allPartNames) {
					for(int i = 0; i < 3; i++) 
					{
						info.partID = name;
						info.abilityID = Random.Range(0,21);
						if((info.abilityID >= 14 && info.abilityID <= 16) || info.abilityID == 3) info.abilityID = 0;
						if(info.abilityID == 10) info.secondaryData = "mini_drone_spawn";
						if(info.abilityID == 0 || info.abilityID == 10) info.tier = 0;
						else info.tier = Random.Range(1, 4);
						parts.Add(info);
					}
				}
			}
			for(int i = 0; i < parts.Count; i++) {
				parts[i] = CullSpatialValues(parts[i]);
			}
		}
		foreach(EntityBlueprint.PartInfo part in parts) {
			if(!partDict.ContainsKey(part)) 
			{
				int size = ResourceManager.GetAsset<PartBlueprint>(part.partID).size;
				ShipBuilderInventoryScript invButton = Instantiate(buttonPrefab, 
					contentsArray[size]).GetComponent<ShipBuilderInventoryScript>();
				partDict.Add(part, invButton);
				contentTexts[size].SetActive(true);
				invButton.part = part;
				invButton.cursor = cursorScript;
				invButton.IncrementCount();
				invButton.mode = BuilderMode.Yard;
			} else partDict[part].IncrementCount();
		}
		if(player.GetTractorTarget() && player.GetTractorTarget().GetComponent<ShellPart>()) {
			var part = player.GetTractorTarget().GetComponent<ShellPart>().info;
			part = CullSpatialValues(part);
			if(!partDict.ContainsKey(part)) {
				int size = ResourceManager.GetAsset<PartBlueprint>(part.partID).size;
				var button = Instantiate(buttonPrefab, contentsArray[size]).GetComponent<ShipBuilderInventoryScript>();
				contentTexts[size].SetActive(true);
				button.cursor = cursorScript;
				button.part = part;
				button.IncrementCount();
				partDict.Add(part, button);
			} else partDict[part].IncrementCount();
			player.cursave.partInventory.Add(part);
			Destroy(player.GetTractorTarget().gameObject);
		}
		LoadBlueprint(player.blueprint);

		// activate windows
		gameObject.SetActive(true);

		if(presetButtons == null || presetButtons.Length == 0) 
		{
			presetButtons = GetComponentsInChildren<PresetButton>();
			foreach(PresetButton button in presetButtons) {
				button.SBPrefab = SBPrefab;
				button.player = player;
				button.cursorScript = cursorScript;
				button.builder = this;
				button.Initialize();
			}
		}

		foreach(PresetButton button in presetButtons) {
			button.gameObject.SetActive(mode == BuilderMode.Yard);
		}

		cursorScript.gameObject.SetActive(true);
		cursorScript.UpdateHandler();
	}

	public void CloseUI() {
		CloseUI(false);
	}

	public void CloseUI(bool validClose) {
		if(!validClose) ResourceManager.PlayClipByID("clip_back");
		else ResourceManager.PlayClipByID("clip_select");
		initialized = false;
		player.SetIsInteracting(false);
		gameObject.SetActive(false);
		if(validClose) {
			player.cursave.partInventory = new List<EntityBlueprint.PartInfo>();
			foreach(EntityBlueprint.PartInfo info in partDict.Keys) {
				if(partDict[info].GetCount() > 0) {
					for(int i = 0; i < partDict[info].GetCount(); i++)
						player.cursave.partInventory.Add(info);
				}
			}
		}
		foreach(ShipBuilderInventoryScript button in partDict.Values) {
			Destroy(button.gameObject);
		}
		foreach(ShipBuilderInventoryScript traderButton in traderPartDict.Values) {
			Destroy(traderButton.gameObject);
		}
		foreach(ShipBuilderPart part in cursorScript.parts) {
			Destroy(part.gameObject);
		}
		cursorScript.parts = new List<ShipBuilderPart>();
		if(!validClose) {
			AbilityHandler handler = player.GetAbilityHandler(); // reset handler to correct representation
			handler.Deinitialize();
			handler.Initialize(player);
		}
	}
	public void LoadBlueprint(EntityBlueprint blueprint) {
		foreach(EntityBlueprint.PartInfo part in blueprint.parts) {
			var p = Instantiate(SBPrefab, cursorScript.transform.parent).GetComponent<ShipBuilderPart>();
			p.cursorScript = cursorScript;
			cursorScript.parts.Add(p);
			p.info = part;
			p.SetLastValidPos(part.location);
			p.isInChain = true;
			p.validPos = true;
		}
	}

	public void Deinitialize() {
		if(cursorScript.buildCost > player.credits) return;
		bool invalidState = false;
		foreach(ShipBuilderPart part in cursorScript.parts) {
			if(!part.validPos || !part.isInChain) {
				invalidState = true;
				break;
			}
		}
		if(!invalidState) {
			Export();
			CloseUI(true);
		}
	}

	public void Export() {
		player.credits -= cursorScript.buildCost;
		player.blueprint.parts = new List<EntityBlueprint.PartInfo>();
		foreach(ShipBuilderPart part in cursorScript.parts) {
			player.blueprint.parts.Add(part.info);
		}
		player.Rebuild();
	}

	void Update() {
		if((player.transform.position - yardPosition).sqrMagnitude > 200)
			CloseUI(false);
	}

	public EntityBlueprint.PartInfo? GetButtonPartCursorIsOn() {
		foreach(ShipBuilderInventoryScript inv in partDict.Values) {
			if(RectTransformUtility.RectangleContainsScreenPoint(inv.GetComponent<RectTransform>(), Input.mousePosition) 
				&& inv.gameObject.activeSelf) {
				return inv.part;
			}
		}
		foreach(ShipBuilderInventoryScript traderInv in traderPartDict.Values) {
			if(RectTransformUtility.RectangleContainsScreenPoint(traderInv.GetComponent<RectTransform>(), Input.mousePosition) 
				&& traderInv.gameObject.activeSelf) {
				return traderInv.part;
			}
		}
		return null;
	}
	public void ChangeDisplayFactors() {
		foreach(GameObject obj in contentTexts) {
			obj.SetActive(false);
		}
		foreach(ShipBuilderInventoryScript inv in partDict.Values) {
			string partName = inv.part.partID.ToLower();
			string abilityName = AbilityUtilities.GetAbilityNameByID(inv.part.abilityID).ToLower();
			if(partName.Contains(searcherString) || abilityName.Contains(searcherString) || searcherString == "") {
				if(displayingTypes[(int)AbilityUtilities.GetAbilityTypeByID(inv.part.abilityID)]) {
					inv.gameObject.SetActive(true);
					contentTexts[ResourceManager.GetAsset<PartBlueprint>(inv.part.partID).size].SetActive(true);
				}
				else inv.gameObject.SetActive(false);
			} else inv.gameObject.SetActive(false);
		}
	}

	public string GetCurrentJSON() {
		EntityBlueprint blueprint = player.blueprint;
		blueprint.parts = new List<EntityBlueprint.PartInfo>();
		foreach(ShipBuilderPart part in cursorScript.parts) {
			blueprint.parts.Add(part.info);
		}
		return JsonUtility.ToJson(blueprint);
	}
	
	public void SetSearcherString(string searcher) {
		searcherString = searcher.ToLower();
		ChangeDisplayFactors();
	}
	public void UpdateDisplayingCategories(int type) {
		displayingTypes[type] = !displayingTypes[type];
		ChangeDisplayFactors();
	}

	public bool GetActive() {
		return gameObject.activeSelf;
	}
}