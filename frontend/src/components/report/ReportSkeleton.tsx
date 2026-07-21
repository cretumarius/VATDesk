function Block({ className }: { className: string }) {
  return <div className={`animate-pulse rounded-lg bg-muted ${className}`} />
}

export function ReportSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading declaration report">
      <Block className="mb-4 h-4 w-36" />
      <div className="mb-8 flex flex-wrap items-start justify-between gap-4">
        <div>
          <Block className="mb-2 h-7 w-48" />
          <Block className="h-4 w-72" />
        </div>
        <div className="flex gap-2.5">
          <Block className="h-11 w-40" />
          <Block className="h-11 w-40" />
        </div>
      </div>

      <div className="mb-6 grid grid-cols-1 gap-3.5 sm:grid-cols-2 lg:grid-cols-4">
        {[0, 1, 2, 3].map((i) => (
          <Block key={i} className="h-[110px]" />
        ))}
      </div>

      <Block className="mb-5 h-14" />
      <Block className="h-72" />
    </div>
  )
}
