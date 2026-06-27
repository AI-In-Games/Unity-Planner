; Soldier domain extended with weapon pickup, used by the runtime demo.
; Adds the weapon-at predicate and the pick-up action so the soldier can start unarmed and must
; fetch a weapon before it can shoot. Everything else matches the benchmark Soldier domain.
(define (domain soldier-armory)
  (:requirements :strips :typing :negative-preconditions)
  (:types agent enemy weapon location)
  (:predicates
    (at ?a - agent ?l - location)
    (connected ?from - location ?to - location)
    (in-cover ?a - agent)
    (cover-at ?l - location)
    (enemy-at ?e - enemy ?l - location)
    (enemy-alive ?e - enemy)
    (has-weapon ?a - agent ?w - weapon)
    (weapon-at ?w - weapon ?l - location)
    (weapon-loaded ?w - weapon)
    (has-grenade ?a - agent)
  )
  (:action move
    :parameters (?a - agent ?from - location ?to - location)
    :precondition (and (at ?a ?from) (connected ?from ?to))
    :effect (and (not (at ?a ?from)) (at ?a ?to) (not (in-cover ?a)))
  )
  (:action pick-up
    :parameters (?a - agent ?w - weapon ?l - location)
    :precondition (and (at ?a ?l) (weapon-at ?w ?l) (not (has-weapon ?a ?w)))
    :effect (and (has-weapon ?a ?w) (not (weapon-at ?w ?l)))
  )
  (:action take-cover
    :parameters (?a - agent ?l - location)
    :precondition (and (at ?a ?l) (cover-at ?l) (not (in-cover ?a)))
    :effect (in-cover ?a)
  )
  (:action reload
    :parameters (?a - agent ?w - weapon)
    :precondition (and (has-weapon ?a ?w) (in-cover ?a) (not (weapon-loaded ?w)))
    :effect (weapon-loaded ?w)
  )
  (:action shoot
    :parameters (?a - agent ?w - weapon ?e - enemy ?l - location)
    :precondition (and (at ?a ?l) (has-weapon ?a ?w) (weapon-loaded ?w) (enemy-at ?e ?l) (enemy-alive ?e))
    :effect (and (not (enemy-alive ?e)) (not (weapon-loaded ?w)))
  )
  (:action throw-grenade
    :parameters (?a - agent ?from - location ?to - location ?e - enemy)
    :precondition (and (at ?a ?from) (has-grenade ?a) (connected ?from ?to) (enemy-at ?e ?to) (enemy-alive ?e))
    :effect (and (not (enemy-alive ?e)) (not (has-grenade ?a)))
  )
)
