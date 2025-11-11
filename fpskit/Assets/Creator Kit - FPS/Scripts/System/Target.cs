﻿﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 5.0f;
    public int pointValue;

    public ParticleSystem DestroyedEffect;
    public ParticleSystem ShieldHitEffect;

    [Header("Audio")]
    public RandomPlayer HitPlayer;
    public AudioSource IdleSource;
    
    [Header("Shield Settings")]
    [Tooltip("Show debug visualization for shielded plane")]
    public bool showDebugRays = true;
    
    [Tooltip("Visual representation of the shield")]
    public GameObject shieldVisual;
    
    public bool Destroyed => m_Destroyed;

    bool m_Destroyed = false;
    float m_CurrentHealth;

    // Dictionary to track shield planes and their directions
    private Dictionary<string, ShieldPlane> m_ShieldPlanes = new Dictionary<string, ShieldPlane>();

    void Awake()
    {
        Helpers.RecursiveLayerChange(transform, LayerMask.NameToLayer("Target"));
    }

    void Start()
    {
        if(DestroyedEffect)
            PoolSystem.Instance.InitPool(DestroyedEffect, 16);
            
        if(ShieldHitEffect)
            PoolSystem.Instance.InitPool(ShieldHitEffect, 8);
        
        m_CurrentHealth = health;
        if(IdleSource != null)
            IdleSource.time = Random.Range(0.0f, IdleSource.clip.length);
            
        // Initialize shield visual
        InitializeShieldVisual();
    }

    void Update()
    {
        // Update shield visual visibility based on debug setting
        if (shieldVisual != null && shieldVisual.activeSelf != showDebugRays)
        {
            shieldVisual.SetActive(showDebugRays);
        }
        
        if (showDebugRays)
        {
            DrawShieldVisualization();
        }
    }

    void InitializeShieldVisual()
    {
        // Create shield visual if not assigned
        if (shieldVisual == null)
        {
            CreateDefaultShieldVisual();
        }
        
        // Set initial visibility
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(showDebugRays);
            
            // Position the shield around the target
            shieldVisual.transform.SetParent(transform);
            shieldVisual.transform.localPosition = Vector3.zero;
            shieldVisual.transform.localRotation = Quaternion.identity;
            shieldVisual.transform.localScale = Vector3.one * 2f; // Perfect cube scale
        }
    }

    void CreateDefaultShieldVisual()
    {
        shieldVisual = new GameObject("ShieldVisual");
        
        // Create separate quads for each shield face with different colors
        CreateShieldFace(Vector3.forward, "Front", Color.red, false);        // Red front (blocking)
        CreateShieldFace(Vector3.up, "Top", Color.green, false);            // Green top (blocking)
        CreateShieldFace(Vector3.down, "Bottom", Color.yellow, false);      // Yellow bottom (blocking)
        CreateShieldFace(Vector3.left, "Left", Color.cyan, false);          // Cyan left (blocking)
        CreateShieldFace(Vector3.right, "Right", Color.magenta, false);     // Magenta right (blocking)
        CreateShieldFace(Vector3.back, "Back", Color.black, true);          // Black back (vulnerable)
    }

    void CreateShieldFace(Vector3 direction, string faceName, Color color, bool isVulnerable)
    {
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = "ShieldFace_" + faceName;
        face.transform.SetParent(shieldVisual.transform);
        
        // Position the face at the appropriate position to form a perfect cube
        face.transform.localPosition = direction * 0.5f;
        
        // Rotate to face outward from the center (fixed orientation)
        face.transform.localRotation = Quaternion.LookRotation(direction);
        
        // Scale to create a perfect cube (1x1 unit quads positioned 0.5 units from center)
        face.transform.localScale = Vector3.one;
        
        // Remove the collider so it doesn't interfere with gameplay
        Collider collider = face.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        
        // Create colored material
        Renderer renderer = face.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material shieldMat = new Material(Shader.Find("Unlit/Color"));
            
            // Make vulnerable shield semi-transparent so it's visible but not obstructive
            if (isVulnerable)
            {
                shieldMat.color = new Color(color.r, color.g, color.b, 0.3f);
            }
            else
            {
                shieldMat.color = color;
            }
            renderer.material = shieldMat;
        }

        // Store shield plane data for vulnerability checking
        m_ShieldPlanes[faceName] = new ShieldPlane
        {
            Direction = direction,
            WorldNormal = transform.TransformDirection(direction),
            IsActive = true,
            IsVulnerable = isVulnerable,
            FaceName = faceName
        };
    }

    void DrawShieldVisualization()
    {
        Vector3 center = transform.position;
        float size = 1f;
        
        // Draw wireframe cube with different colors for each face
        Vector3[] corners = {
            center + new Vector3(-size, -size, -size),
            center + new Vector3(size, -size, -size),
            center + new Vector3(size, size, -size),
            center + new Vector3(-size, size, -size),
            center + new Vector3(-size, -size, size),
            center + new Vector3(size, -size, size),
            center + new Vector3(size, size, size),
            center + new Vector3(-size, size, size)
        };
        
        // Draw front face (red)
        Debug.DrawLine(corners[4], corners[5], Color.red);
        Debug.DrawLine(corners[5], corners[6], Color.red);
        Debug.DrawLine(corners[6], corners[7], Color.red);
        Debug.DrawLine(corners[7], corners[4], Color.red);
        
        // Draw top face (green)
        Debug.DrawLine(corners[2], corners[6], Color.green);
        Debug.DrawLine(corners[3], corners[7], Color.green);
        
        // Draw bottom face (yellow)
        Debug.DrawLine(corners[0], corners[4], Color.yellow);
        Debug.DrawLine(corners[1], corners[5], Color.yellow);
        
        // Draw left face (cyan)
        Debug.DrawLine(corners[0], corners[3], Color.cyan);
        Debug.DrawLine(corners[4], corners[7], Color.cyan);
        
        // Draw right face (magenta)
        Debug.DrawLine(corners[1], corners[2], Color.magenta);
        Debug.DrawLine(corners[5], corners[6], Color.magenta);

        // Draw back face (black) with X to mark vulnerability
        DrawBackVulnerabilityX();

        // Draw normals for each shield plane
        foreach (var shieldPlane in m_ShieldPlanes.Values)
        {
            Vector3 planeCenter = transform.position + transform.TransformDirection(shieldPlane.Direction) * 0.5f;
            Color normalColor = shieldPlane.IsVulnerable ? Color.black : Color.white;
            Debug.DrawRay(planeCenter, shieldPlane.WorldNormal * 0.5f, normalColor);
        }
    }

    void DrawBackVulnerabilityX()
    {
        Vector3 backCenter = transform.position + transform.TransformDirection(Vector3.back) * 0.5f;
        float backSize = 1f;
        
        // Calculate the four corners of the back plane
        Vector3 backTopLeft = backCenter + 
                            (transform.up * backSize * 0.5f) - 
                            (transform.right * backSize * 0.5f);
        
        Vector3 backTopRight = backCenter + 
                             (transform.up * backSize * 0.5f) + 
                             (transform.right * backSize * 0.5f);
        
        Vector3 backBottomLeft = backCenter - 
                               (transform.up * backSize * 0.5f) - 
                               (transform.right * backSize * 0.5f);
        
        Vector3 backBottomRight = backCenter - 
                                (transform.up * backSize * 0.5f) + 
                                (transform.right * backSize * 0.5f);

        // Draw the X - diagonal lines crossing the entire back plane
        Debug.DrawLine(backTopLeft, backBottomRight, Color.black);
        Debug.DrawLine(backTopRight, backBottomLeft, Color.black);

        // Draw the border of the vulnerable back area
        Debug.DrawLine(backTopLeft, backTopRight, Color.black);
        Debug.DrawLine(backTopRight, backBottomRight, Color.black);
        Debug.DrawLine(backBottomRight, backBottomLeft, Color.black);
        Debug.DrawLine(backBottomLeft, backTopLeft, Color.black);
    }

    public void Got(float damage, Vector3 damageSourcePosition)
    {
        Vector3 damageDirection = (transform.position - damageSourcePosition).normalized;

        // Find which shield plane was hit
        ShieldPlane hitShield = GetHitShieldPlane(damageDirection);
        
        if (hitShield != null)
        {
            Debug.Log($"Hit shield: {hitShield.FaceName}, Vulnerable: {hitShield.IsVulnerable}, Dot: {Vector3.Dot(hitShield.WorldNormal, -damageDirection)}");
            
            if (hitShield.IsVulnerable)
            {
                // Vulnerable shield hit - apply damage
                Debug.Log("Vulnerable shield hit - applying damage!");
                ApplyDamage(damage);
            }
            else
            {
                // Regular shield hit - block damage
                Debug.Log("Protected shield hit - blocking damage!");
                BlockDamage(damageSourcePosition);
            }
        }
        else
        {
            // No shield hit (edge case) - apply damage
            Debug.Log("No shield hit - applying damage!");
            ApplyDamage(damage);
        }
    }

    public void Got(float damage)
    {
        BlockDamage(transform.position + transform.forward);
    }

    ShieldPlane GetHitShieldPlane(Vector3 damageDirection)
    {
        // Update all shield normals first
        foreach (var shieldPlane in m_ShieldPlanes.Values)
        {
            shieldPlane.WorldNormal = transform.TransformDirection(shieldPlane.Direction);
        }

        ShieldPlane bestMatch = null;
        float highestDot = 0f;

        // Find the shield plane that best matches the damage direction
        foreach (var shieldPlane in m_ShieldPlanes.Values)
        {
            float dot = Vector3.Dot(shieldPlane.WorldNormal, -damageDirection);
            
            // Debug visualization for the check
            if (showDebugRays)
            {
                Vector3 planeCenter = transform.position + shieldPlane.WorldNormal * 0.5f;
                Debug.DrawRay(planeCenter, -damageDirection * 1f, 
                            dot > 0.7f ? Color.yellow : Color.gray, 0.1f);
            }
            
            if (dot > 0.7f && dot > highestDot)
            {
                highestDot = dot;
                bestMatch = shieldPlane;
            }
        }

        return bestMatch;
    }

    void BlockDamage(Vector3 hitPosition)
    {
        if(HitPlayer != null)
            HitPlayer.PlayRandom();
            
        if(ShieldHitEffect != null)
        {
            var effect = PoolSystem.Instance.GetInstance<ParticleSystem>(ShieldHitEffect);
            effect.time = 0.0f;
            effect.Play();
            effect.transform.position = hitPosition;
            
            Vector3 directionToSource = (hitPosition - transform.position).normalized;
            if (directionToSource != Vector3.zero)
                effect.transform.rotation = Quaternion.LookRotation(directionToSource);
        }
    }

    void ApplyDamage(float damage)
    {
        m_CurrentHealth -= damage;
        
        if(HitPlayer != null)
            HitPlayer.PlayRandom();
        
        if(m_CurrentHealth > 0)
            return;

        Vector3 position = transform.position;
        
        if (HitPlayer != null)
        {
            var source = WorldAudioPool.GetWorldSFXSource();
            source.transform.position = position;
            source.pitch = HitPlayer.source.pitch;
            source.PlayOneShot(HitPlayer.GetRandomClip());
        }

        if (DestroyedEffect != null)
        {
            var effect = PoolSystem.Instance.GetInstance<ParticleSystem>(DestroyedEffect);
            effect.time = 0.0f;
            effect.Play();
            effect.transform.position = position;
        }

        m_Destroyed = true;
        
        gameObject.SetActive(false);
       
        GameSystem.Instance.TargetDestroyed(pointValue);
    }

    // Helper class to store shield plane data
    [System.Serializable]
    private class ShieldPlane
    {
        public Vector3 Direction;
        public Vector3 WorldNormal;
        public bool IsActive;
        public bool IsVulnerable;
        public string FaceName;
    }
}