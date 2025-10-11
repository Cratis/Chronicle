---
applyTo: "**/for_*/**/*.*, **/when_*/**/*.*"
---

# 🧪 How to Write Specs

We call automated tests for specs or specifications based on Specification by Example related to BDD (Behavior Driven Development).
Keep tests focused, isolated, and descriptive!

## Structure

- **File/Folder Structure:**
  - Organize tests by feature/domain, e.g. `Events/Constraints/for_UniqueConstraintProvider/when_providing`.
  - Use descriptive folder and file names:
    - `for_<TypeUnderTest>/` for the unit under test
    - `when_<behavior>/` for behaviors with multiple outcomes
    - Single file `when_<behavior>` for simple behaviors with single outcomes
    - Example: `for_UnitOfWork/when_committing/and_it_has_events_and_append_returns_constraints_and_errors`
 
## What to specify

- Write specs that verify what is promised from the signature of methods, not based on its implementation - we want to catch problems with the implementation.
- **Focus on behaviors (methods that perform actions)**, not simple state (properties that hold or return values).
- **Ignore simple properties** - properties that just return constructor parameters, field values, or delegate to other objects without transformation should not have specs.
- Do not test logging - it's too fragile and has no value.

## Naming

- Use clear, descriptive names for test classes and methods.
- Folder: `for_<Unit>`.
- Class: Single condition - `when_<action>[_and_<aspect>]`. Use descriptive prepositions that make the condition clear:
  - `and_*` for additional conditions or compound scenarios
  - `or_*` for alternative outcomes or error paths
  - `with_*`, `without_*`, `when_*`, `having_*`, `given_*` for specific contexts
  - Example: `when_committing_and_validation_fails`, `when_processing_with_invalid_data`
- Method: `should_<expected_result>`

## Behaviors

- Capture the behavior in naming, not just necessarily use names that reflect the code or method name, it should be meaningful as to what the code is doing and trying to accomplish
- **A behavior corresponds to a public method** of the unit under test
- Every public method should be tested separately, never write a single file for an entire unit under test
- Each behavior (public method) should have its own dedicated folder structure when it has multiple outcomes
- For simple behaviors with single outcomes, use a single file: `when_<behavior>.cs`
- For complex behaviors with multiple aspects/outcomes, use folder structure: `when_<behavior>/` with multiple test files inside

## Multiple outcomes of a behavior

- When a behavior (public method) has multiple outcomes, aspects, or edge cases to test, organize them using folder structure:
  - Create a folder named `when_<behavior>` (e.g., `when_checking_can_handle`, `when_handling`)
  - Within this folder, create separate test files for each specific outcome or aspect
  - Use descriptive names that clearly indicate the specific condition being tested:
    - `with_valid_events_collection`
    - `with_null_value`
    - `without_event_source_id`
    - `empty_events_collection`
    - `multiple_events_collection`
- Do not limit naming to `and_*` - use any preposition or descriptor that makes the condition clear:
  - `and_*` for additional conditions (e.g., `and_it_has_events`, `and_validation_fails`)
  - `or_*` for alternative scenarios (e.g., `or_timeout_occurs`, `or_connection_fails`)
  - `with_*` for scenarios with specific data/state (e.g., `with_valid_events_collection`, `with_null_value`)
  - `without_*` for scenarios missing data/state (e.g., `without_event_source_id`, `without_permissions`)
  - `when_*` for temporal or conditional aspects (e.g., `when_timeout_occurs`, `when_validation_passes`)
  - `having_*` for possession/state-based conditions (e.g., `having_multiple_items`, `having_no_data`)
  - `given_*` for precondition scenarios (e.g., `given_empty_collection`, `given_invalid_state`)
- Keep a single specific outcome or aspect in each file - don't combine multiple scenarios
- Each test file should inherit from the appropriate reusable context (usually `given.a_<unit_name>`)

## Reusable Context

- Context can be encapsulated into reusable contexts that can be leveraged between specs.
- Create a `given` folder within the unit folder (e.g., `for_<Unit>/given/`)
- Add reusable context classes with descriptive names starting with `a_` or `an_` (e.g., `a_events_command_response_value_handler`)

- **For behaviors with multiple outcomes:**
  - The reusable context should be in the unit's `given/` folder (e.g., `for_<Unit>/given/`)
  - All test files within behavior folders (e.g., `when_<behavior>/`) inherit from this shared context
  - This allows consistent setup across all variations of the behavior
- If a specific behavior needs additional setup, create behavior-specific contexts in `when_<behavior>/given/` folder

## Formatting

- Don't worry about lines being too long for the `should_` methods - prefer one-line lambda methods for readability of the should statements rather than breaking them into multiple lines.
- With multiple `should_` methods, do not add unnecessary whitespace between them.

## Properties and Simple Members

**NEVER write specs for simple properties or getters** - Properties are not behaviors and should not be tested unless they contain complex business logic.

**Avoid file names starting with:**
- `when_getting_*` (e.g., `when_getting_key.*`, `when_getting_properties.*`)
- `when_returning_*` for simple return values
- Any spec that just verifies a property returns what was passed in the constructor

Focus specs on **behaviors** (methods that perform actions) rather than **state** (properties that hold or return values).
