﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SapphSky.CoreFramework {
	/// <summary>
	/// Handles the Character's Foot IK system.
	/// </summary>
	public class CharacterFootIK : MonoBehaviour {
		[Header("Foot IK Parameters")]
		public bool useFootIK = true;
		public LayerMask footIKLayer;
		public float raycastHeightFromGround = 1.25f, raycastDownDistance = 1.5f, pelvisOffset;
		public float feetToIKDeltaSpeed = 0.2f, pelvisDeltaSpeed = 0.5f;
		public string lFootAnimVarName = "LeftFootCurve", rFootAnimVarName = "RightFootCurve";
		private Animator animator;
		private Vector3 leftFootPos, rightFootPos;
		private Vector3 leftFootIKPos, rightFootIKPos;
		private Quaternion leftFootIKRot, rightFootIKRot;
		private float lastRightFootYPos, lastLeftFootYPos, lastPelvisYPos;

		private void Awake() {
			if (animator == null) {
				animator = GetComponent<Animator>();
			}
		}

		private void FixedUpdate() {
			if (useFootIK == false || animator == null) {
				return;
			}

			AdjustFeetTarget(ref leftFootPos, HumanBodyBones.LeftFoot);
			AdjustFeetTarget(ref rightFootPos, HumanBodyBones.RightFoot);

			FeetPosSolver(leftFootPos, ref leftFootIKPos, ref leftFootIKRot);
			FeetPosSolver(rightFootPos, ref rightFootIKPos, ref rightFootIKRot);
		}

		private void OnAnimatorIK(int layerIndex) {
			if (useFootIK == false || animator == null) {
				return;
			}

			MovePelvisToHeight();

			// Set Foot IK Position and Rotation.
			animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(lFootAnimVarName));
			MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, leftFootIKPos, leftFootIKRot, ref lastLeftFootYPos);

			animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
			animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat(rFootAnimVarName));
			MoveFeetToIKPoint(AvatarIKGoal.RightFoot, rightFootIKPos, rightFootIKRot, ref lastRightFootYPos);
		}

		private void MoveFeetToIKPoint(AvatarIKGoal foot, Vector3 positionIKHolder, Quaternion rotationIKHolder, ref float lastFootYPos) {
			Vector3 targetIKPosition = animator.GetIKPosition(foot);
			if (positionIKHolder != Vector3.zero) {
				targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
				positionIKHolder = transform.InverseTransformPoint(positionIKHolder);

				float yVar = Mathf.Lerp(lastFootYPos, positionIKHolder.y, feetToIKDeltaSpeed);
				targetIKPosition.y += yVar;
				lastFootYPos = yVar;
				targetIKPosition = transform.TransformPoint(targetIKPosition);
				animator.SetIKRotation(foot, rotationIKHolder);
			}
			animator.SetIKPosition(foot, targetIKPosition);
		}

		/// <summary>
		/// Adjusts the pelvis' height to compensate for the ground's height difference.
		/// </summary>
		private void MovePelvisToHeight() {
			if (rightFootIKPos == Vector3.zero || leftFootIKPos == Vector3.zero || lastPelvisYPos == 0) {
				lastPelvisYPos = animator.bodyPosition.y;
				return;
			}

			float lOffsetPosition = leftFootIKPos.y - transform.position.y;
			float rOffsetPosition = rightFootIKPos.y - transform.position.y;
			float totalOffet = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;
			Vector3 newPelvisPos = animator.bodyPosition + Vector3.up * totalOffet;
			newPelvisPos.y = Mathf.Lerp(lastPelvisYPos, newPelvisPos.y, pelvisDeltaSpeed);
			animator.bodyPosition = newPelvisPos;
			lastPelvisYPos = animator.bodyPosition.y;
		}

		/// <summary>
		/// Draws raycasts from the feet down to determine position and rotation.
		/// </summary>
		/// <param name="fromSkyPos"></param>
		/// <param name="feetIKPos"></param>
		/// <param name="feetIKRot"></param>
		private void FeetPosSolver(Vector3 fromSkyPos, ref Vector3 feetIKPos, ref Quaternion feetIKRot) {
			RaycastHit feetOutHit;
			Debug.DrawLine(fromSkyPos, fromSkyPos + Vector3.down * (raycastDownDistance + raycastHeightFromGround), Color.yellow);

			if (Physics.Raycast(fromSkyPos, Vector3.down, out feetOutHit, raycastDownDistance + raycastHeightFromGround)) {
				feetIKPos = fromSkyPos;
				feetIKPos.y = feetOutHit.point.y + pelvisOffset;
				feetIKRot = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation;
				return;
			}

			feetIKPos = Vector3.zero;   // If this happens, it didn't work. (Raycast couldn't find ground)
		}

		private void AdjustFeetTarget(ref Vector3 feetPos, HumanBodyBones foot) {
			feetPos = animator.GetBoneTransform(foot).position;
			feetPos.y = transform.position.y + raycastHeightFromGround;
		}
	}
}
