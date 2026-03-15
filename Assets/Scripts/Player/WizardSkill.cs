using UnityEngine;
using System.Collections;

public class WizardSkill : MonoBehaviour
{
    [Header("Meteor Strike (Ultimate)")]
    public GameObject meteorPrefab;
    public GameObject aoeIndicatorPrefab;
    public float cooldown = 12f;
    public float radius = 6f;
    public LayerMask groundLayer;

    private bool isSelecting;
    private bool onCooldown;
    private GameObject currentIndicator;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !onCooldown)
        {
            if (isSelecting) CancelSelection();
            else StartSelection();
        }

        if (isSelecting)
        {
            UpdateIndicator();
            if (Input.GetMouseButtonDown(0)) CastMeteor();
        }
    }

    private void StartSelection()
    {
        isSelecting = true;
        if (aoeIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(aoeIndicatorPrefab);
            currentIndicator.transform.localScale = new Vector3(radius, 0.1f, radius);
        }
    }

    private void UpdateIndicator()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            if (currentIndicator != null) currentIndicator.transform.position = hit.point;
        }
    }

    private void CastMeteor()
    {
        Vector3 targetPos = currentIndicator != null ? currentIndicator.transform.position : transform.position + transform.forward * 5f;
        CancelSelection();

        // เล่นอนิเมชั่นถ้ามี Animator
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Ultimate");

        if (meteorPrefab != null)
        {
            Vector3 spawnPos = targetPos + Vector3.up * 20f;
            Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
        }

        StartCoroutine(CooldownRoutine());
    }

    private void CancelSelection()
    {
        isSelecting = false;
        if (currentIndicator != null) Destroy(currentIndicator);
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
