(define (domain companion)
  (:requirements :strips :typing :negative-preconditions)
  (:types
    agent
    enemy
    weapon
    item
    zone
  )
  (:predicates
    (in-zone ?a - agent ?z - zone)
    (has-weapon ?a - agent ?w - weapon)
    (weapon-equipped ?a - agent ?w - weapon)
    (enemy-alive ?e - enemy)
    (engaging ?a - agent ?e - enemy)
    (health-low ?a - agent)
    (has-item ?a - agent ?i - item)
    (item-available ?i - item)
    (is-safe-zone ?z - zone)
    (is-combat-zone ?z - zone)
  )
  (:action move-to-zone
    :parameters (?a - agent ?from - zone ?to - zone)
    :precondition (in-zone ?a ?from)
    :effect (and (not (in-zone ?a ?from)) (in-zone ?a ?to))
  )
  (:action equip-weapon
    :parameters (?a - agent ?w - weapon)
    :precondition (and (has-weapon ?a ?w) (not (weapon-equipped ?a ?w)))
    :effect (weapon-equipped ?a ?w)
  )
  (:action use-item
    :parameters (?a - agent ?i - item)
    :precondition (and (has-item ?a ?i) (health-low ?a))
    :effect (and (not (has-item ?a ?i)) (not (health-low ?a)))
  )
  (:action engage-enemy
    :parameters (?a - agent ?e - enemy ?z - zone)
    :precondition (and (in-zone ?a ?z) (is-combat-zone ?z) (enemy-alive ?e) (not (engaging ?a ?e)) (not (health-low ?a)))
    :effect (engaging ?a ?e)
  )
  (:action attack-enemy
    :parameters (?a - agent ?e - enemy ?w - weapon)
    :precondition (and (engaging ?a ?e) (weapon-equipped ?a ?w) (enemy-alive ?e))
    :effect (and (not (enemy-alive ?e)) (not (engaging ?a ?e)))
  )
  (:action retreat
    :parameters (?a - agent ?from - zone ?to - zone)
    :precondition (and (in-zone ?a ?from) (is-combat-zone ?from) (is-safe-zone ?to))
    :effect (and (not (in-zone ?a ?from)) (in-zone ?a ?to))
  )
  (:action rest
    :parameters (?a - agent ?z - zone)
    :precondition (and (in-zone ?a ?z) (is-safe-zone ?z) (health-low ?a))
    :effect (not (health-low ?a))
  )
  (:action pick-up-item
    :parameters (?a - agent ?i - item ?z - zone)
    :precondition (and (in-zone ?a ?z) (is-safe-zone ?z) (item-available ?i) (not (has-item ?a ?i)))
    :effect (and (has-item ?a ?i) (not (item-available ?i)))
  )
)