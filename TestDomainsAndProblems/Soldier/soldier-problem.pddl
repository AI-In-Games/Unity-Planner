(define (problem soldier-problem)
  (:domain soldier)
  (:objects
    soldier1 - agent
    enemy1 - enemy
    rifle1 - weapon
    entrance corridor bunker - location
  )
  (:init
    (at soldier1 entrance)
    (has-weapon soldier1 rifle1)
    (weapon-loaded rifle1)
    (enemy-at enemy1 bunker)
    (enemy-alive enemy1)
    (connected entrance corridor)
    (connected corridor bunker)
  )
  (:goal
    (not (enemy-alive enemy1))
  )
)
