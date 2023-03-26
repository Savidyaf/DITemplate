using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "ScriptableObjects/GameStateManager/Settings")]
public class GameSettings : ScriptableObject
{
    public PreLoadedGameSettings DefaultGameSettings;
}