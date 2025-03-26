using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is an example script with intentional issues for testing the AI Coding Assistant
public class ExampleScript : MonoBehaviour
{
    // Missing SerializeField attribute on private field that should be editable
    private float moveSpeed = 5f;
    
    // Unused variable
    private Vector3 lastPosition;
    
    // Public variable that should be private with SerializeField
    public int health = 100;
    
    // Inefficient string concatenation in Update method
    private string playerStatus = "";
    
    void Start()
    {
        // Missing null check before accessing component
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.mass = 2f;
        
        // Debug log without conditional compilation
        Debug.Log("Player initialized with health: " + health);
        
        lastPosition = transform.position;
    }
    
    void Update()
    {
        // Inefficient input handling
        if (Input.GetKeyDown(KeyCode.W))
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        
        if (Input.GetKeyDown(KeyCode.S))
            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
            
        if (Input.GetKeyDown(KeyCode.A))
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
            
        if (Input.GetKeyDown(KeyCode.D))
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        
        // Inefficient string concatenation in Update
        playerStatus = "Position: " + transform.position.x + ", " + transform.position.y + ", " + transform.position.z;
        
        // Finding object by name in Update method
        GameObject mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
        {
            // Do something with the camera
        }
    }
    
    // Method with obvious performance issues
    private void CheckNearbyObjects()
    {
        // Using FindObjectsOfType in a potentially frequently called method
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < 10f)
            {
                Debug.Log("Object nearby: " + obj.name);
            }
        }
    }
    
    // Method with redundant code
    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        
        if (health <= 0)
        {
            health = 0;
            Debug.Log("Player died!");
        }
        
        if (health <= 0)
        {
            // Redundant check
            DestroyPlayer();
        }
    }
    
    private void DestroyPlayer()
    {
        // Direct call to Destroy instead of using gameObject reference
        Destroy(this);
    }
} 