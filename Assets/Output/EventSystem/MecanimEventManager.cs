using UnityEngine;
using System.Collections.Generic;

public static class MecanimEventManager {
	private static MecanimEventData[] eventDataSources;
	
	private static Dictionary<int, Dictionary<int, Dictionary<int, List<MecanimEvent>>>> loadedData;
	
	private static Dictionary<int, Dictionary<int, AnimatorStateInfo>> lastStates = new Dictionary<int, Dictionary<int, AnimatorStateInfo>>();
	
	
	public static void SetEventDataSource(MecanimEventData dataSource) {
		if (dataSource != null) {
			eventDataSources = new MecanimEventData[1];
			eventDataSources[0] = dataSource;
			
			LoadDataInGame();
		}
	}
	
	public static void SetEventDataSource(MecanimEventData[] dataSources) {
		if (dataSources != null) {
			
			eventDataSources = dataSources;
			
			LoadDataInGame();
		}
	}
	
	public static void OnLevelLoaded() {
		lastStates.Clear();
	}
	
	public static MecanimEvent[] GetEvents(int animatorControllerId, Animator animator) {
		List<MecanimEvent> allEvents = new List<MecanimEvent>();
		
		int animatorHash = animator.GetHashCode();
		if (!lastStates.ContainsKey(animatorHash))
			lastStates[animatorHash] = new Dictionary<int, AnimatorStateInfo>();
		
		int layerCount = animator.layerCount;
		
		Dictionary<int, AnimatorStateInfo> lastLayerState = lastStates[animatorHash];
		
		for (int layer = 0; layer < layerCount; layer++) {
			if (!lastLayerState.ContainsKey(layer))
				lastLayerState[layer] = new AnimatorStateInfo();
			
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
			
			int lastLoop = (int)lastLayerState[layer].normalizedTime;
			int currLoop = (int)stateInfo.normalizedTime;
			float lastNormalizedTime = lastLayerState[layer].normalizedTime - lastLoop;
			float currNormalizedTime = stateInfo.normalizedTime - currLoop;
			
			if (lastLayerState[layer].nameHash == stateInfo.nameHash) {
				if (stateInfo.loop == true) {
					if (lastLoop == currLoop) {
						allEvents.AddRange(CollectEvents(animator, animatorControllerId, layer, stateInfo.nameHash, lastNormalizedTime, currNormalizedTime));
					}
					else {
						allEvents.AddRange(CollectEvents(animator, animatorControllerId, layer, stateInfo.nameHash, lastNormalizedTime, 1.0f));
						allEvents.AddRange(CollectEvents(animator, animatorControllerId, layer, stateInfo.nameHash, 0.0f, currNormalizedTime));
					}
				}
				else {
					float start = Mathf.Clamp01(lastLayerState[layer].normalizedTime);
					float end = Mathf.Clamp01(stateInfo.normalizedTime);
					
					if (start != end)
						allEvents.AddRange(CollectEvents(animator, animatorControllerId, layer, stateInfo.nameHash, start, end));
				}
			}
			else {
				allEvents.AddRange(CollectEvents(animator, animatorControllerId, layer, stateInfo.nameHash, 0.0f, currNormalizedTime));
			}
			
			lastLayerState[layer] = stateInfo;
		}
		
		return allEvents.ToArray();
	}
	
	private static MecanimEvent[] CollectEvents(Animator animator, int animatorControllerId, int layer, int nameHash, float normalizedTimeStart, float normalizedTimeEnd) {
		List<MecanimEvent> events;
		
		if (loadedData.ContainsKey(animatorControllerId) &&
			loadedData[animatorControllerId].ContainsKey(layer) && 
			loadedData[animatorControllerId][layer].ContainsKey(nameHash)) {
			
			events = loadedData[animatorControllerId][layer][nameHash];
		}
		else {
			return new MecanimEvent[0];
		}
		
		List<MecanimEvent> ret = new List<MecanimEvent>();
		
		foreach (MecanimEvent e in events) {
			if (e.normalizedTime >= normalizedTimeStart && e.normalizedTime < normalizedTimeEnd) {
				if (e.condition.Test(animator))
					ret.Add(new MecanimEvent(e));
			}
		}
		
		return ret.ToArray();
	}
	
	private static void LoadDataInGame() {
		
		if (eventDataSources == null)
			return;
		
		loadedData = new Dictionary<int, Dictionary<int, Dictionary<int, List<MecanimEvent>>>>();
		
		foreach (MecanimEventData dataSource in eventDataSources) {
		
			if (dataSource == null)
				continue;
			
			MecanimEventDataEntry[] entries = dataSource.data;
			
			foreach(MecanimEventDataEntry entry in entries) {
				int animatorControllerId = entry.animatorController.GetInstanceID();
				
				if (!loadedData.ContainsKey(animatorControllerId))
					loadedData[animatorControllerId] = new Dictionary<int, Dictionary<int, List<MecanimEvent>>>();
				
				if (!loadedData[animatorControllerId].ContainsKey(entry.layer)) {
					loadedData[animatorControllerId][entry.layer] = new Dictionary<int, List<MecanimEvent>>();
				}
				
				loadedData[animatorControllerId][entry.layer][entry.stateNameHash] = new List<MecanimEvent>(entry.events);
			}
			
		}
	}
	
}
