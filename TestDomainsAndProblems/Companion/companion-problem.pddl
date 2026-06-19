(define (problem companion-clear-enemies)
  (:domain companion)
  (:objects
    companion1 - agent
    enemy1 - enemy
    enemy2 - enemy
    sword - weapon
    bow - weapon
    potion1 - item
    zone-safe - zone
    zone-combat - zone
  )
  (:init
    (in-zone companion1 zone-safe)
    (health-low companion1)
    (enemy-alive enemy1)
    (enemy-alive enemy2)
    (has-weapon companion1 sword)
    (has-weapon companion1 bow)
    (item-available potion1)
    (is-safe-zone zone-safe)
    (is-combat-zone zone-combat)
  )
  (:goal (and (not (enemy-alive enemy1)) (not (enemy-alive enemy2)) (in-zone companion1 zone-safe)))
)