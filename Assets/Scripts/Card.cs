﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Audio;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Action<Card> OnCardAnimationFinished;

    [SerializeField] private Transform rotationRoot = null;

    [Header("Power Texts")]
    [SerializeField] private TextMeshPro powerUpText = null;
    [SerializeField] private TextMeshPro powerDownText = null;
    [SerializeField] private TextMeshPro powerLeftText = null;
    [SerializeField] private TextMeshPro powerRightText = null;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer cardImage = null;
    [SerializeField] private SpriteRenderer cardBackground = null;

    [Header("Player Colors")]
    [SerializeField] private Color playerOneColor = default(Color);
    [SerializeField] private Color playerTwoColor = default(Color);

    [Header("Sounds")]
    [SerializeField] private AudioClip selectCardSound = null;
    [SerializeField] private AudioClip turnCardSound = null;

    [Header("Card Datas")]
    [SerializeField] private CardDatas datas;
    public CardDatas Datas {
        get {
            return datas;
        }
        set {
            if (datas != value) {
                datas = value;

                UpdatePowers();
                UpdateView();
            }
        }
    }

    private Animator animator;
    private Animator Animator {
        get {
            if (animator == null) {
                animator = GetComponent<Animator>();
            }

            return animator;
        }
    }

    private SpriteRenderer spriteRenderer;
    public SpriteRenderer SpriteRenderer {
        get {
            if (spriteRenderer == null) {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            return spriteRenderer;
        }
    }

    public bool Interactable { get; set; } = true;

    private PlayerNumber playerOwner = PlayerNumber.None;
    public PlayerNumber PlayerOwner {
        get { return playerOwner; }
        set {
            if (playerOwner != value) {
                playerOwner = value;
                switch (playerOwner) {
                    case PlayerNumber.One:
                        cardBackground.color = playerOneColor;
                        break;
                    case PlayerNumber.Two:
                        cardBackground.color = playerTwoColor;
                        break;
                    default:
                        cardBackground.color = Color.gray;
                        break;
                }

                newPlayerOwner = playerOwner;
            }
        }
    }

    private PlayerNumber newPlayerOwner = PlayerNumber.None;

    private Dictionary<CardDirection, CardPower> cardPowersByDirection = new Dictionary<CardDirection, CardPower>();

    private float zDistanceToCamera = 0;

    private Vector3 beforeDragPosition = Vector3.zero;

    private SelectableArea currentAreaSelected = null;

    public CardPower GetPowerByDirection(CardDirection _targetDirection) {
        return cardPowersByDirection[_targetDirection];
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (Interactable) {
            Animator.SetInteger("OverPlayer", int.Parse(PlayerOwner.ToString("d")));
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (Interactable) {
            Animator.SetInteger("OverPlayer", 0);
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (Interactable) {
            Animator.SetInteger("OverPlayer", 0);

            beforeDragPosition = transform.localPosition;
            zDistanceToCamera = Mathf.Abs(beforeDragPosition.z - Camera.main.transform.position.z);

            transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistanceToCamera));

            AudioManager.Instance.PlaySound(selectCardSound);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (Interactable) {
            transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistanceToCamera));

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            SelectableArea hitArea = null;
            foreach (RaycastResult raycastItem in raycastResults) {
                hitArea = raycastItem.gameObject.GetComponent<SelectableArea>();
                if (hitArea != null) {
                    break;
                }
            }

            if (currentAreaSelected != hitArea) {
                if (currentAreaSelected != null) {
                    currentAreaSelected.OnPointerExit(eventData);
                }

                currentAreaSelected = hitArea;

                if (currentAreaSelected != null) {
                    currentAreaSelected.OnPointerEnter(eventData);
                }
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (Interactable) {
            Animator.SetInteger("OverPlayer", 0);

            transform.localPosition = beforeDragPosition;

            if (currentAreaSelected != null) {
                currentAreaSelected.Card = this;
                currentAreaSelected = null;
            }

            AudioManager.Instance.PlaySound(selectCardSound);
        }
    }

    private void OnValidate() {
        UpdateView();
    }

    private void Start() {
        UpdatePowers();
        UpdateView();
    }

    public bool IsLooseBattle(CardDirection _targetDirection, CardPower _powerToCompare, PlayerNumber _opponentPlayer) {
        bool isPlayerOwnerChanged = (playerOwner != _opponentPlayer && cardPowersByDirection[_targetDirection] < _powerToCompare);
        if (isPlayerOwnerChanged) {
            newPlayerOwner = _opponentPlayer;
            StartRotationAnimation(_targetDirection);

            AudioManager.Instance.PlaySound(turnCardSound);
        }

        return isPlayerOwnerChanged;
    }

    private void StartRotationAnimation(CardDirection _targetDirection) {
        string formattedRotationTrigger = string.Format("Rotate{0}", _targetDirection.ToString());
        Animator.SetTrigger(formattedRotationTrigger);
    }

    private void UpdatePowers() {
        if (datas != null) {
            cardPowersByDirection[CardDirection.Up] = datas.PowerUp;
            cardPowersByDirection[CardDirection.Down] = datas.PowerDown;
            cardPowersByDirection[CardDirection.Left] = datas.PowerLeft;
            cardPowersByDirection[CardDirection.Right] = datas.PowerRight;
        }
    }

    private void UpdateView() {
        if (datas != null) {
            cardImage.sprite = datas.SpriteImage;

            powerUpText.text = FormatPower(datas.PowerUp);
            powerDownText.text = FormatPower(datas.PowerDown);
            powerLeftText.text = FormatPower(datas.PowerLeft);
            powerRightText.text = FormatPower(datas.PowerRight);
        }
        else {
            Debug.LogWarning("No card datas set to update view", this);
        }
    }

    private string FormatPower(CardPower _power) {
        return _power == CardPower.Ace ? "A" : _power.ToString("d");
    }

    // Animation Event Functions
    private void UpdatePlayerOwner() {
        PlayerOwner = newPlayerOwner;
    }

    private void RotationAnimationFinished() {
        if (OnCardAnimationFinished != null) {
            OnCardAnimationFinished(this);
        }
    }
}
