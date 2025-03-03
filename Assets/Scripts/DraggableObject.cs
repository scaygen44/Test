using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class DraggableObject : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private float dragZDepth = -0.5f;
    [SerializeField] private float defaultZDepth = 0f;
    [SerializeField] private float moveSmoothness = 15f;
    [SerializeField] private float stackSpacing = 0.4f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask shelfLayer;
    [SerializeField] private LayerMask appleLayer;

    [Header("Visual Settings")]
    [SerializeField] private int normalSortOrder = 2;
    [SerializeField] private int dragSortOrder = 10;

    // Компоненты
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    
    // Состояние
    private bool isDragging = false;
    private Vector3 offset;
    private float currentZDepth;
    private Transform currentShelf;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        // Игнорируем коллизии между яблоками
        Physics2D.IgnoreLayerCollision(
            gameObject.layer, 
            LayerMask.NameToLayer("Apples"), 
            true
        );

        InitializeObject();
    }

    void InitializeObject()
    {
        currentZDepth = defaultZDepth;
        rb.gravityScale = gravityScale;
        rb.isKinematic = false;
        spriteRenderer.sortingOrder = normalSortOrder;
    }

    void OnMouseDown()
    {
        if (isDragging) return;

        Vector3 mousePos = GetMouseWorldPos();
        offset = transform.position - mousePos;
        StartDragging();
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 targetPosition = GetMouseWorldPos() + offset;
        targetPosition.z = dragZDepth;

        transform.position = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            Time.deltaTime * moveSmoothness
        );
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        StopDragging();
    }

    private void StartDragging()
    {
        isDragging = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        rb.gravityScale = 0;
        spriteRenderer.sortingOrder = dragSortOrder;
        currentZDepth = dragZDepth;
    }

    private void StopDragging()
    {
        isDragging = false;
        rb.isKinematic = false;
        rb.gravityScale = gravityScale;
        spriteRenderer.sortingOrder = normalSortOrder;
        CheckForShelf();
    }

    private void CheckForShelf()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.2f, shelfLayer);
        
        if (hits.Length > 0)
        {
            SnapToShelf(hits[0].transform);
        }
        else
        {
            currentZDepth = defaultZDepth;
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                defaultZDepth
            );
        }
    }

    private void SnapToShelf(Transform shelf)
    {
        currentShelf = shelf;
        Vector3 newPosition = CalculateStackPosition(shelf);
        
        transform.position = new Vector3(
            newPosition.x,
            newPosition.y,
            shelf.position.z
        );

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        currentZDepth = shelf.position.z;
    }

    private Vector3 CalculateStackPosition(Transform shelf)
    {
        // Считаем количество яблок на этой полке
        int appleCount = 0;
        Collider2D[] apples = Physics2D.OverlapCircleAll(
            shelf.position, 
            1f, 
            appleLayer
        );

        foreach (Collider2D apple in apples)
        {
            if (apple.transform != transform && apple.CompareTag("Apple"))
                appleCount++;
        }

        // Рассчитываем позицию с отступом
        return new Vector3(
            shelf.position.x + (appleCount * stackSpacing),
            shelf.position.y,
            shelf.position.z
        );
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z + currentZDepth);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}