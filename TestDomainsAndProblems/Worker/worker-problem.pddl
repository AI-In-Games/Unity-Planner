(define (problem worker-deliver-log)
  (:domain worker)
  (:objects
    worker1 - worker
    log-01 - resource
    loc-start - location
    loc-forest - location
    loc-depot - location
  )
  (:init
    (at worker1 loc-start)
    (resource-at log-01 loc-forest)
    (is-depot loc-depot)
  )
  (:goal (delivered log-01))
)