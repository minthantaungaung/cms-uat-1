# Base image
FROM clamav/clamav:stable

# Temporarily switch to root to install bash
USER root
RUN apk add --no-cache bash

# Switch back to the non-root "clamav" user
USER clamav

# Expose the ClamAV port
EXPOSE 3310

# Use the unprivileged entrypoint script
ENTRYPOINT ["/init-unprivileged"]

# Default command for ClamD
CMD ["clamd"]
