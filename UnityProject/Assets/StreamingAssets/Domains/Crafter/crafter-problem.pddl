(define (problem crafter-problem)
  (:domain crafter)
  (:objects
    crafter1 - agent
    wilderness workshop - location
  )
  (:init
    (at crafter1 wilderness)
    (has-pickaxe crafter1)
    (wood-available wilderness)
    (ore-available wilderness)
    (connected wilderness workshop)
    (workbench-at workshop)
  )
  (:goal
    (has-iron-sword crafter1)
  )
)
