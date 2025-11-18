# Performance Optimization Workflow

Applies when user asks for optimization or benchmarking.

## 1. Baseline
- Use latest stable release tag (`vX.Y.Z`) as baseline.
- If none exists, fallback to `main` HEAD.

## 2. Benchmark Storage
- Save benchmark markdown under `/benchmarks/<slug>.md`.
- Store benchmark source under `/benchmarks/src/`.

## 3. Commit
Include in commit message:
- Purpose
- Optimization details
- Benchmark summary

## 4. Tagging
- Add lightweight tag: `perf/<task-slug>`
- Do not create release version unless user requests.
