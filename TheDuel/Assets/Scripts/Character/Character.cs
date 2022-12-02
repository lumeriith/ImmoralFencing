using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public bool canAct => isIdle && !isStunned && !isSwitchingWeapon;

    public Action<Item> onAddItem;
    public Action<Item> onRemoveItem;
    public Action<Item> onChangeEquippedItem;
    public Action<InfoAttackHit> onDealAttack;
    public Action<InfoAttackHit> onTakeAttack;
    public Action<bool> onCheatingChanged;

    public Item[] defaultItemPrefabs;
    public List<Item> items;
    public Item equippedItem;
    public int maxItems = 7;

    public const float maxStamina = 100f;
    public float stamina = 100f;
    public const float minStamina = 0f;

    public double basicAgility = 1.0;
    public const double agilityRate = 1.0;
    private double _lastTotalAgility;

    public const float maxHealth = 100f;
    public float health = 100f;
    public const float decreaseHealthRate = 0.01f;
    public const float minHealth = 0f;

    public const float staminaRecoveryRate = 0.1f;

    public bool canAddItem => items.Count < maxItems;

    private Item _defaultItem;

    public bool isCheating { get; private set; }
    public bool isAiming { get; protected set; }
    public bool isDodging { get; private set; }
    public bool isStunned => _currentStunDuration > 0;

    private byte _isCheatingCounter;
    public Animator animator { get; private set; }
    public ModelActionInput modelActionInput { get; private set; }
    private float _currentStunDuration;

    [NonSerialized]
    public bool isIdle;
    public bool isSwitchingWeapon { get; private set; }

    public float dodgeDuration = 0.5f;
    public float dodgeStaminaCost = 20f;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        modelActionInput = GetComponent<ModelActionInput>();
        _lastTotalAgility = basicAgility + (maxHealth - health) * agilityRate;
    }

    protected virtual void Start()
    {
        if (defaultItemPrefabs != null)
        {
            foreach (var w in defaultItemPrefabs)
            {
                var weap = AddItem(w);
                if (_defaultItem == null)
                {
                    _defaultItem = weap;
                    EquipDefaultItem();
                }
            }
        }

        GameManager.instance.onRoundPrepare += EquipDefaultItem;
        GameManager.instance.onRoundPrepare += PlayDrawSword;
        GameManager.instance.onRoundPrepare += () => stamina = maxStamina;
    }

    protected virtual void Update()
    {
        _currentStunDuration = Mathf.MoveTowards(_currentStunDuration, 0, Time.deltaTime);
        animator.SetBool("IsStunned", isStunned);

        if (stamina < maxStamina) AddStamina(staminaRecoveryRate * health * Time.deltaTime);

        double totalAgility = GetTotalAgility();
        if (_lastTotalAgility != totalAgility && modelActionInput != null)
        {
            modelActionInput.UpdateTotalAgility(totalAgility);
            _lastTotalAgility = totalAgility;
        }
    }

    public void EquipDefaultItem()
    {
        if (_defaultItem == null)
        {
            Debug.LogWarning("Default item not given", this);
            return;
        }
        EquipItem(_defaultItem);
    }

    public Item AddItem(Item prefab)
    {
        if (!canAddItem) throw new InvalidOperationException("Max item limit reached");
        var newItem = Instantiate(prefab, transform);
        newItem.owner = this;
        items.Add(newItem);
        onAddItem?.Invoke(newItem);
        return newItem;
    }

    public void EquipItem(Item item)
    {
        if (equippedItem != null) UnequipItem();
        item.isEquipped = true;
        equippedItem = item;
        item.OnEquip();
        onChangeEquippedItem?.Invoke(item);
    }

    public void UnequipItem()
    {
        if (equippedItem == null) throw new InvalidOperationException();
        equippedItem.OnUnequip();
        equippedItem = null;
    }

    public void RemoveItem(Item item)
    {
        if (item == null || !items.Contains(item)) throw new InvalidOperationException();

        if (equippedItem == item)
        {
            UnequipItem();
            StartCoroutine(SwitchWeaponRoutine(EquipDefaultItem));
        }

        items.Remove(item);
        onRemoveItem?.Invoke(item);
        Destroy(item.gameObject);
    }

    public void UseItem()
    {
        if (equippedItem == null || !equippedItem.isUseReady) return;
        if (stamina >= equippedItem.requireStamina && canAct)
        {
            equippedItem.Use();
            AddStamina(-equippedItem.requireStamina);
            AddHealth(-equippedItem.requireStamina * decreaseHealthRate);
        }
    }

    public void CycleItemUp()
    {
        if (!canAct) return;
        if (items.Count < 2) return;
        var i = items.IndexOf(equippedItem);
        if (i == -1) return;
        i++;
        if (i >= items.Count) i = 0;
        SwitchToItemAtIndex(i);
    }

    public void CycleItemDown()
    {
        if (!canAct) return;
        if (items.Count < 2) return;
        var i = items.IndexOf(equippedItem);
        if (i == -1) return;
        i--;
        if (i < 0) i = items.Count - 1;
        SwitchToItemAtIndex(i);
    }

    public void SwitchToItemAtIndex(int index)
    {
        if (index == items.IndexOf(equippedItem)) return;
        if (!HasItemAt(index)) return;
        if (!canAct) return;
        isSwitchingWeapon = true;
        StartCoroutine(SwitchWeaponRoutine(() => EquipItem(items[index])));
    }

    private IEnumerator SwitchWeaponRoutine(Action callback)
    {
        PlaySwitch();
        isSwitchingWeapon = true;
        yield return new WaitForSeconds(0.4f);
        callback?.Invoke();
        yield return new WaitForSeconds(0.4f);
        isSwitchingWeapon = false;
    }

    public bool HasItemAt(int index)
    {
        return index >= 0 && index < items.Count;
    }

    public void IncrementCheatingCounter()
    {
        if (_isCheatingCounter == byte.MaxValue) throw new InvalidOperationException();
        _isCheatingCounter++;
        UpdateCheatingState();
    }

    public void DecrementCheatingCounter()
    {
        if (_isCheatingCounter == byte.MinValue) throw new InvalidOperationException();
        _isCheatingCounter--;
        UpdateCheatingState();
    }

    private void UpdateCheatingState()
    {
        if (isCheating != _isCheatingCounter > 0)
        {
            onCheatingChanged?.Invoke(_isCheatingCounter > 0);
            isCheating = _isCheatingCounter > 0;
        }
    }

    public void Stun(float duration)
    {
        _currentStunDuration = Mathf.Max(_currentStunDuration, duration);
    }

    public void PlayHitHead()
    {
        animator.SetTrigger("GetHitHead");
    }

    public void PlayHitFront()
    {
        isIdle = false;
        animator.SetTrigger("GetHitFront");
    }

    public void PlayHitBack()
    {
        isIdle = false;
        animator.SetTrigger("GetHitBack");
    }

    public void PlayPickUp()
    {
        isIdle = false;
        animator.SetTrigger("PickUp");
    }

    public void PlayUseRemote()
    {
        isIdle = false;
        animator.SetTrigger("UseRemote");
    }

    public void PlayThrow()
    {
        isIdle = false;
        animator.SetTrigger("Throw");
    }

    public void PlayDodge()
    {
        animator.SetTrigger("Dodge");
    }

    public void PlayDrawSword()
    {
        isIdle = false;
        animator.SetTrigger("DrawSword");
    }

    public void PlaySwitch()
    {
        animator.SetTrigger("Switch");
    }

    public void PlayBasicAttack()
    {
        isIdle = false;
        animator.SetTrigger("Basic Attack");
    }
    
    public void PlayLeapAttack()
    {
        isIdle = false;
        animator.SetTrigger("Leap Attack");
    }

    public void SetStamina(float val)
    {
        stamina = val;
        if (stamina > maxStamina) stamina = maxStamina;
        if (stamina < minStamina) stamina = minStamina;
    }

    public void AddStamina(float val)
    {
        stamina += val;
        if (stamina > maxStamina) stamina = maxStamina;
        if (stamina < minStamina) stamina = minStamina;
    }

    public float GetStamina()
    {
        return stamina;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetMinHealth()
    {
        return minHealth;
    }

    public double GetAgilityRate()
    {
        return agilityRate;
    }

    public void Dodge()
    {
        if (!canAct) return;
        if (stamina < dodgeStaminaCost) return;
        StartCoroutine(DodgeRoutine());
        IEnumerator DodgeRoutine()
        {
            isDodging = true;
            AddStamina(-dodgeStaminaCost);
            PlayDodge();
            yield return new WaitForSeconds(dodgeDuration);
            isDodging = false;
        }
    }

    public double GetTotalAgility()
    {
        return basicAgility + (maxHealth - health) * agilityRate;
    }

    public void SetHealth(float val)
    {
        health = val;
        if (health > maxHealth) health = maxHealth;
        if (health < minHealth) health = minHealth;
    }

    public void AddHealth(float val)
    {
        health += val;
        if (health > maxHealth) health = maxHealth;
        if (health < minHealth) health = minHealth;
    }

    public float GetHealth()
    {
        return health;
    }
}
