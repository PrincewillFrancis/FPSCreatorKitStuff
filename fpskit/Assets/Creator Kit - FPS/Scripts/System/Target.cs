﻿using System.Collections;
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
            
            // Position the shield in front of the target
            shieldVisual.transform.SetParent(transform);
            shieldVisual.transform.localPosition = new Vector3(0, 0, 0.5f);
            shieldVisual.transform.localRotation = Quaternion.identity;
            shieldVisual.transform.localScale = Vector3.one * 1.2f;
        }
    }

    void CreateDefaultShieldVisual()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shieldVisual.name = "ShieldVisual";
        
        // Remove the collider so it doesn't interfere with gameplay
        Collider collider = shieldVisual.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        
        // Create a semi-transparent blue material
        Renderer renderer = shieldVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material shieldMat = new Material(Shader.Find("Standard"));
            shieldMat.color = new Color(0, 0.5f, 1f, 0.3f);
            shieldMat.SetFloat("_Mode", 2); // Fade mode
            shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shieldMat.SetInt("_ZWrite", 0);
            shieldMat.DisableKeyword("_ALPHATEST_ON");
            shieldMat.EnableKeyword("_ALPHABLEND_ON");
            shieldMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            shieldMat.renderQueue = 3000;
            renderer.material = shieldMat;
        }
    }

    void DrawShieldVisualization()
    {
        Vector3 shieldCenter = transform.position + transform.forward * 0.5f;
        float shieldSize = 1f;
        
        Vector3 topLeft = shieldCenter + (transform.up * shieldSize * 0.5f) - (transform.right * shieldSize * 0.5f);
        Vector3 topRight = shieldCenter + (transform.up * shieldSize * 0.5f) + (transform.right * shieldSize * 0.5f);
        Vector3 bottomLeft = shieldCenter - (transform.up * shieldSize * 0.5f) - (transform.right * shieldSize * 0.5f);
        Vector3 bottomRight = shieldCenter - (transform.up * shieldSize * 0.5f) + (transform.right * shieldSize * 0.5f);
        
        Debug.DrawLine(topLeft, topRight, Color.blue);
        Debug.DrawLine(topRight, bottomRight, Color.blue);
        Debug.DrawLine(bottomRight, bottomLeft, Color.blue);
        Debug.DrawLine(bottomLeft, topLeft, Color.blue);
        
        Debug.DrawLine(topLeft, bottomRight, Color.blue);
        Debug.DrawLine(topRight, bottomLeft, Color.blue);
    }

    public void Got(float damage, Vector3 damageSourcePosition)
    {
        Vector3 damageDirection = (transform.position - damageSourcePosition).normalized;

        if (IsVulnerableFromDirection(damageDirection))
        {
            ApplyDamage(damage);
        }
        else
        {
            BlockDamage(damageSourcePosition);
        }
    }

    public void Got(float damage)
    {
        BlockDamage(transform.position + transform.forward);
    }

    bool IsVulnerableFromDirection(Vector3 damageDirection)
    {
        float dot = Vector3.Dot(-transform.forward, damageDirection);

        return dot > 0.7f;
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
}