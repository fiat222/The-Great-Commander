using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    public int health = 100;

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"<color=green>[Base]</color> HP : {health}");

        if (health <= 0)
        {
            Debug.LogError("ฐานพังแล้ว! จบเกม");
        }
    }
}