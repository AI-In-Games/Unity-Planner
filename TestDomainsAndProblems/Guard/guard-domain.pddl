(define (domain guard)
  (:requirements :strips :typing :negative-preconditions)
  (:types
    guard
    waypoint
    invpoint
    suspect
    alarm
  )
  (:predicates
    (at ?g - guard ?wp - waypoint)
    (on-patrol ?g - guard)
    (suspicious ?g - guard)
    (investigating ?g - guard)
    (pursuing ?g - guard)
    (suspect-visible ?g - guard)
    (suspect-in-area ?s - suspect)
    (target-acquired ?g - guard ?s - suspect)
    (alarm-raised ?a - alarm)
    (backup-called ?g - guard)
    (at-investigate-point ?g - guard ?pt - invpoint)
  )
  (:action patrol
    :parameters (?g - guard ?from - waypoint ?to - waypoint)
    :precondition (and (at ?g ?from) (on-patrol ?g))
    :effect (and (not (at ?g ?from)) (at ?g ?to))
  )
  (:action start-investigate
    :parameters (?g - guard ?pt - invpoint)
    :precondition (and (suspicious ?g) (not (at-investigate-point ?g ?pt)))
    :effect (and (not (on-patrol ?g)) (investigating ?g) (at-investigate-point ?g ?pt))
  )
  (:action clear-suspicion
    :parameters (?g - guard ?pt - invpoint)
    :precondition (and (investigating ?g) (at-investigate-point ?g ?pt) (not (suspect-visible ?g)))
    :effect (and (not (suspicious ?g)) (not (investigating ?g)) (not (at-investigate-point ?g ?pt)))
  )
  (:action spot-suspect
    :parameters (?g - guard ?s - suspect)
    :precondition (and (investigating ?g) (suspect-in-area ?s))
    :effect (and (suspect-visible ?g) (target-acquired ?g ?s))
  )
  (:action pursue-suspect
    :parameters (?g - guard ?s - suspect)
    :precondition (and (target-acquired ?g ?s) (not (pursuing ?g)))
    :effect (and (pursuing ?g) (not (investigating ?g)))
  )
  (:action raise-alarm
    :parameters (?g - guard ?a - alarm ?s - suspect)
    :precondition (and (pursuing ?g) (target-acquired ?g ?s) (not (alarm-raised ?a)))
    :effect (and (alarm-raised ?a) (not (pursuing ?g)))
  )
  (:action resume-patrol
    :parameters (?g - guard ?wp - waypoint)
    :precondition (and (not (suspicious ?g)) (not (on-patrol ?g)) (not (investigating ?g)) (not (pursuing ?g)))
    :effect (and (on-patrol ?g) (at ?g ?wp))
  )
  (:action call-backup
    :parameters (?g - guard)
    :precondition (and (pursuing ?g) (not (backup-called ?g)))
    :effect (backup-called ?g)
  )
  (:action lose-suspect
    :parameters (?g - guard ?s - suspect)
    :precondition (and (pursuing ?g) (target-acquired ?g ?s) (not (suspect-in-area ?s)))
    :effect (and (not (target-acquired ?g ?s)) (not (pursuing ?g)) (not (suspect-visible ?g)))
  )
)