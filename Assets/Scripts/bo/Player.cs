using System.Collections;
using System.Collections.Generic;

using UnityEngine;


public class Player : MonoBehaviour
{
    public float movingSpeed = 1;

    private Animator animator;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isAttacking = false;
    private float attackTime = float.MinValue;

    [HideInInspector]
    public PlayerData playerData;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        MoveUpdate();
        AttackUpdate();
    }


    public void MoveTo(Vector3 pos)
    {
        //Debug.Log("I want to move to " + pos);
        targetPosition = pos;
        transform.LookAt(pos);
        animator.SetBool("isMoving", true);
        isMoving = true;
    }

    void MoveUpdate()
    {
        Debug.Log("MoveUpdate");

        if (isMoving)
        {
            Vector3 pos = transform.position;
            transform.position = Vector3.MoveTowards(pos, targetPosition, movingSpeed * Time.deltaTime);

            if (Vector3.Distance(pos, targetPosition) < 0.05f)
            {
                animator.SetBool("isMoving", false);
                isMoving = false;
                transform.position = targetPosition;
                Debug.Log("移动结束坐标：" + transform.position + "目标坐标：" + targetPosition);

            }
        }

    }

    public void Attack()
    {
        isAttacking = true;
        attackTime = Time.time;
        animator.SetBool("isAttacking", true);
    }

    void AttackUpdate()
    {
        Debug.Log("AttackUpdate");
        if (isAttacking)
        {
            if (Time.time - attackTime < 1.2f)
            {
                return;
            }
            isAttacking = false;
            animator.SetBool("isAttacking", false);
        }

    }

}
