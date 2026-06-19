(define (problem guard-raise-alarm-and-backup)
  (:domain guard)
  (:objects
    guard1 - guard
    wp-a - waypoint
    wp-b - waypoint
    wp-c - waypoint
    inv-pt1 - invpoint
    inv-pt2 - invpoint
    suspect1 - suspect
    alarm1 - alarm
  )
  (:init
    (at guard1 wp-a)
    (suspicious guard1)
    (suspect-in-area suspect1)
  )
  (:goal (and (alarm-raised alarm1) (backup-called guard1)))
)