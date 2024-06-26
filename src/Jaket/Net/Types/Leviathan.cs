namespace Jaket.Net.Types;

using UnityEngine;

using Jaket.Content;
using Jaket.IO;
using Jaket.World;

/// <summary> Leviathan representation responsible for synchronizing animations and attacks. </summary>
public class Leviathan : Entity
{
    /// <summary> Boss controller containing references to the head and tail of Leviathan. </summary>
    private LeviathanController levi;

    /// <summary> Leviathan health, position and rotation. </summary>
    private FloatLerp health, headX, headY, headZ, tailX, tailY, tailZ, headRotation, tailRotation;
    /// <summary> Head and tail positions used to synchronize attacks. </summary>
    public byte HeadPos, LastHeadPos, TailPos, LastTailPos;

    private void Awake()
    {
        Init(_ => EntityType.Leviathan);

        health = new();
        headX = new(); headY = new(); headZ = new();
        tailX = new(); tailY = new(); tailZ = new();
        headRotation = new();
        tailRotation = new();

        levi = GetComponent<LeviathanController>();
        EnemyId = levi.eid;
        Animator = levi.head.GetComponent<Animator>();

        if (LobbyController.IsOwner)
            LobbyController.ScaleHealth(ref levi.stat.health);
        else
        {
            levi.active = levi.head.active = false;
            health.target = levi.eid.health;
        }

        World.Instance.Leviathan = this;
    }

    private void Update()
    {
        if (LobbyController.IsOwner) return;

        EnemyId.health = levi.stat.health = health.Get(LastUpdate);
        EnemyId.dead = levi.stat.health <= 0f;

        levi.head.transform.position = new(headX.Get(LastUpdate), headY.Get(LastUpdate), headZ.Get(LastUpdate));
        levi.tail.transform.position = new(tailX.Get(LastUpdate), tailY.Get(LastUpdate), tailZ.Get(LastUpdate));
        levi.head.transform.localEulerAngles = new(0f, headRotation.Get(LastUpdate), 0f);
        levi.tail.transform.localEulerAngles = new(0f, tailRotation.Get(LastUpdate), 0f);

        if (LastHeadPos != HeadPos)
        {
            LastHeadPos = HeadPos;

            switch (HeadPos)
            {
                case 0:
                    Animator.SetBool("ProjectileBurst", levi.head.active = true);
                    Animator.SetBool("Sunken", false);
                    break;
                case 1:
                    Animator.SetBool("ProjectileBurst", levi.head.active = false);
                    Animator.SetBool("Sunken", false);

                    Animator.SetTrigger("Bite");
                    break;
                case 0xFF: // -1 when converted to byte gives 0xFF
                    Animator.SetBool("ProjectileBurst", levi.head.active = false);
                    Animator.SetBool("Sunken", true);
                    break;
            }
        }

        if (LastTailPos != TailPos)
        {
            LastTailPos = TailPos;

            // needed to trigger an animation and attack
            levi.tail.ChangePosition();
        }
    }

    #region entity

    public override void Write(Writer w)
    {
        w.Float(levi.eid.health);
        w.Vector(levi.head.transform.position);
        w.Vector(levi.tail.transform.position);
        w.Float(levi.head.transform.localEulerAngles.y);
        w.Float(levi.tail.transform.localEulerAngles.y);

        w.Bool(levi.head.gameObject.activeSelf);
        w.Bool(levi.tail.gameObject.activeSelf);

        w.Byte(HeadPos);
        w.Byte(TailPos);
    }

    public override void Read(Reader r)
    {
        LastUpdate = Time.time;

        health.Read(r);
        headX.Read(r); headY.Read(r); headZ.Read(r);
        tailX.Read(r); tailY.Read(r); tailZ.Read(r);
        headRotation.Read(r);
        tailRotation.Read(r);

        levi.head.gameObject.SetActive(r.Bool());
        levi.tail.gameObject.SetActive(r.Bool());

        HeadPos = r.Byte();
        TailPos = r.Byte();
    }

    #endregion
}
