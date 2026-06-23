using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerBack : MonoBehaviour
{
    public float movingSpeed = 1;
    private Animator animator;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isAttacking = false;

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


    void OnMove(InputValue value)
    {
        Debug.Log("I want to move");
        if (value.isPressed) {
			Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
			RaycastHit hit;
			Physics.Raycast(ray, out hit);
			if (hit.collider.tag == "Terrain"){
				Debug.Log("I want to move to " + hit.point);
                transform.LookAt(hit.point);
                targetPosition = hit.point;
                animator.SetBool("isMoving", true); 
                isMoving = true;
			} 
		}	
    }

    void MoveUpdate()
    {

        if (isMoving){
            Vector3 pos = transform.position;
		    transform.position = Vector3.MoveTowards(pos, targetPosition, movingSpeed * Time.deltaTime);

            if(Vector3.Distance(pos, targetPosition) < 0.05f){
			    animator.SetBool("isMoving", false);
                isMoving = false;
            }
        }
		
    }
    /***********************************************************/
    void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
                Debug.Log("I want to attack you " );        
                animator.SetBool("isAttacking", true);
                isAttacking = true;

        }
    }

    void AttackUpdate()
    {
        if (isAttacking)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 0.7f)
            {
                //              ò   ֵΪfalse
                animator.SetBool("isAttacking", false);
            }
        }

    }

}
