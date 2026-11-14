using System;
using UnityEngine;

namespace FirstGame
{
    public enum TargetingRule { ClosestToBase, Nearest, Strongest }
    public enum AttackAnimationStyle { Recoil, Pulse, Glow }

    [Serializable]
    public sealed class TowerLevelConfig
    {
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;
        public int damage = 10;
        public float attackRate = 1f;
        public float range = 2.5f;
        public int upgradeCost = 50;
        public float projectileSpeed = 8f;
        public float effectRadius = 1.25f;
        public float damageDelay;
    }

    [CreateAssetMenu(menuName = "First Game/Tower Config")]
    public sealed class TowerConfig : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject towerPrefab;
        public int buildCost = 50;
        public TargetingRule targetingRule;
        public AttackAnimationStyle animationStyle;
        public Color rangeColor = new(0.3f, 1f, 0.5f, 0.16f);
        public AudioClip attackSound;
        public AudioClip hitSound;
        public GameObject attackEffectPrefab;
        public GameObject hitEffectPrefab;
        public TowerLevelConfig[] levels = { new(), new() };
    }
}
