(define (domain worker)
  (:requirements :strips :typing :negative-preconditions)
  (:types
    worker
    resource
    location
  )
  (:predicates
    (at ?w - worker ?loc - location)
    (resource-at ?r - resource ?loc - location)
    (has-resource ?w - worker ?r - resource)
    (delivered ?r - resource)
    (is-depot ?loc - location)
  )
  (:action move
    :parameters (?w - worker ?from - location ?to - location)
    :precondition (at ?w ?from)
    :effect (and (not (at ?w ?from)) (at ?w ?to))
  )
  (:action gather
    :parameters (?w - worker ?r - resource ?loc - location)
    :precondition (and (at ?w ?loc) (resource-at ?r ?loc) (not (has-resource ?w ?r)))
    :effect (and (has-resource ?w ?r) (not (resource-at ?r ?loc)))
  )
  (:action deliver
    :parameters (?w - worker ?r - resource ?loc - location)
    :precondition (and (at ?w ?loc) (is-depot ?loc) (has-resource ?w ?r))
    :effect (and (not (has-resource ?w ?r)) (delivered ?r))
  )
)