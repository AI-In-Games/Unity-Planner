(define (problem heist-problem)
  (:domain heist)
  (:objects
    thief1 - agent
  )
  (:init
    (has-noise-maker thief1)
  )
  (:goal
    (escaped thief1)
  )
)
