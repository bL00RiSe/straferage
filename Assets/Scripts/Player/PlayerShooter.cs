﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

//Should be on PlayerMover
public class PlayerShooter : MonoBehaviour
{
    public enum WeaponTypes { GUN = 0, SHOTGUN, MACHINE_GUN, ROCKET, SNIPER_GUN }

    public Light WeaponSpark;
    public AudioSource PistolSound;
    public AudioSource ShotgunSound;
    public AudioSource RocketSound;
    public WeaponTypes SelectedWeapon;
    public float ZoomFOV = 20f;
    
    private Dictionary<WeaponTypes, float> shootingDelays = new Dictionary<WeaponTypes, float>() {
        { WeaponTypes.GUN, .5f },
        { WeaponTypes.SHOTGUN, 1f },
        { WeaponTypes.MACHINE_GUN, .2f },
        { WeaponTypes.ROCKET, 2f },
        { WeaponTypes.SNIPER_GUN, 2f }
    };
    private Dictionary<WeaponTypes, float> shootingPowers = new Dictionary<WeaponTypes, float>() {
        { WeaponTypes.GUN, 5f },
        { WeaponTypes.SHOTGUN, 5f },
        { WeaponTypes.MACHINE_GUN, 5f },
        { WeaponTypes.ROCKET, 50f },
        { WeaponTypes.SNIPER_GUN, 30f }
    };

    private static PlayerShooter instance;
    private Camera myCam;

    private bool fire = false;
    private float lastShootTime;
    private float normalFOV;
    private float targetFOV = 60f;
    private float rocketPower = 0f;

    void Awake()
    {
        instance = this;
        myCam = GetComponent<Camera>();
        normalFOV = myCam.fieldOfView;
    }

    private bool delayEnded { get { return ((lastShootTime + shootingDelays[SelectedWeapon]) < Time.time); } }
    private bool canShoot { get { return (fire && delayEnded); } }

    void Update()
    {
        if (fire)
        {
            switch (SelectedWeapon)
            {
                case WeaponTypes.GUN:
                case WeaponTypes.MACHINE_GUN:
                    if (delayEnded)
                    {
                        PistolSound.Play();
                        ShootWithRaycast(false, shootingPowers[SelectedWeapon]);
                        StartCoroutine(Spark());
                        lastShootTime = Time.time;
                    }
                    break;
                case WeaponTypes.ROCKET:
                    rocketPower = Mathf.Lerp(rocketPower, 1f, Time.deltaTime);
                    break;
                case WeaponTypes.SNIPER_GUN:
                    targetFOV = ZoomFOV;
                    break;
            }
        }

        myCam.fieldOfView = Mathf.Lerp(myCam.fieldOfView, targetFOV, Time.deltaTime * 10f);
    }

    private void StartShootInternal()
    {
        fire = true;
        
        switch (SelectedWeapon)
        {
            case WeaponTypes.MACHINE_GUN:
                lastShootTime = Time.time;
                break; //Fake the first shot for machine gun
            case WeaponTypes.SHOTGUN:
                if (delayEnded)
                {
                    ShotgunSound.Play();
                    StartCoroutine(Spark());
                    for (int i = 0; i < 5; i++) { ShootWithRaycast(true, shootingPowers[SelectedWeapon]); }
                }
                break;
        }
    }

    private void EndShootInternal()
    {
        if (canShoot)
        {
            switch (SelectedWeapon)
            {
                case WeaponTypes.SNIPER_GUN:
                    targetFOV = normalFOV;
                    PistolSound.Play();
                    StartCoroutine(Spark());
                    ShootWithRaycast(false, shootingPowers[SelectedWeapon]);
                    lastShootTime = Time.time;
                    break;
                case WeaponTypes.SHOTGUN:
                    ShotgunSound.Play();
                    StartCoroutine(Spark());
                    for (int i = 0; i < 5; i++) { ShootWithRaycast(true, shootingPowers[SelectedWeapon]); }
                    lastShootTime = Time.time;
                    break;
                case WeaponTypes.ROCKET:
                    RocketSound.Play();
                    Projectile.Fire<Rocket>(shootingPowers[SelectedWeapon], transform.TransformPoint(Vector3.forward), transform.rotation, gameObject).SetPower(rocketPower);
                    lastShootTime = Time.time;
                    rocketPower = 0f;
                    break;
            }
        }

        fire = false;
    }

    private void ShootWithRaycast(bool randomizeDirection, float power)
    {
        Vector3 direction = transform.forward + (randomizeDirection ? UnityEngine.Random.onUnitSphere * .05f : Vector3.zero);
        Array.ForEach(Physics.RaycastAll(transform.position, direction), hit =>
        {
            Enemy shotEnemy = (hit.collider ? hit.collider.GetComponent<Enemy>() : null);
            if (shotEnemy != null)
            {
                shotEnemy.ApplyShot(power, hit.point, direction);
            }
            Instantiate(PrefabAccessor.Instance.DustParticle, hit.point, Quaternion.identity);
        });
    }

    private void ChangeWeapon(int direction)
    {
        WeaponTypes[] arr = (WeaponTypes[])Enum.GetValues(typeof(WeaponTypes));
        if (((int)SelectedWeapon + direction) == (arr.Length))
        {
            SelectedWeapon = 0;
            return;
        }
        if (((int)SelectedWeapon + direction) < 0)
        {
            SelectedWeapon = arr[arr.Length - 1];
            return;
        }
        SelectedWeapon = arr[(int)SelectedWeapon + direction];
    }

    private IEnumerator Spark()
    {
        WeaponSpark.enabled = true;
        yield return new WaitForSeconds(.1f);
        WeaponSpark.enabled = false;
    }

    public static void StartShoot() { instance.StartShootInternal(); }
    public static void EndShoot() { instance.EndShootInternal(); }
    public static void NextWeapon() { instance.ChangeWeapon(1); }
    public static void PrevWeapon() { instance.ChangeWeapon(-1); }
}
